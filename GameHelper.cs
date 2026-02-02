using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace FrameHunterFPS
{
    public class GameEntry : System.ComponentModel.INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string InstallLocation { get; set; }
        public string Source { get; set; }
        public bool ShowInjectButton { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        private string _imageUrl = "https://via.placeholder.com/600x900/161B22/00F2FF?text=LOADING...";
        public string ImageUrl
        {
            get => _imageUrl;
            set { _imageUrl = value; OnPropertyChanged(nameof(ImageUrl)); }
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public static class GameHelper
    {
        private static readonly HttpClient client = new HttpClient();
        private const string API_KEY = "e84f456b2a8a4125709e43ed4f778914";
        private static readonly string CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FrameHunter", "Cache");

        public static List<GameEntry> GetAllGames()
        {
            if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
            var allGames = new List<GameEntry>();

            // 1. Launchers Específicos
            allGames.AddRange(GetSteamGames());
            allGames.AddRange(GetEpicGames());
            allGames.AddRange(GetRiotGames()); // Lógica nova aplicada aqui

            // 2. Battle.net (Pasta Padrão)
            allGames.AddRange(GetBattleNetGames());

            // 3. Varredura Universal via Registro (Backup)
            allGames.AddRange(GetRegistryGames());

            var blacklist = new[] { "vanguard", "anti-cheat", "steamworks", "redistributable", "framework", "update", "driver", "runtime", "common redist", "agent", "launcher", "riot client" };

            return allGames
                .Where(g => !string.IsNullOrWhiteSpace(g.Name))
                .Where(g => !string.IsNullOrWhiteSpace(g.InstallLocation))
                .Where(g => !blacklist.Any(b => g.Name.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0))
                .GroupBy(g => g.Name)
                .Select(g => g.First())
                .ToList();
        }

        // --- CORREÇÃO: Nova lógica de detecção da Riot via JSON oficial ---
        private static List<GameEntry> GetRiotGames()
        {
            var games = new List<GameEntry>();
            try
            {
                // Caminho padrão onde o Riot Client salva a lista de jogos instalados
                string jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Riot Games", "RiotClientInstalls.json");

                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
                            {
                                string productKey = property.Name;
                                string installPath = property.Value.GetString();

                                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                                {
                                    string friendlyName = "";
                                    if (productKey.Equals("valorant", StringComparison.OrdinalIgnoreCase)) friendlyName = "VALORANT";
                                    else if (productKey.Equals("league_of_legends", StringComparison.OrdinalIgnoreCase)) friendlyName = "League of Legends";
                                    else if (productKey.Contains("lor")) friendlyName = "Legends of Runeterra";

                                    // Se achou um jogo conhecido, adiciona
                                    if (!string.IsNullOrEmpty(friendlyName))
                                    {
                                        games.Add(new GameEntry { Name = friendlyName, InstallLocation = installPath, Source = "Riot Client" });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // Fallback: Se o JSON falhar, tenta varredura manual básica nos discos
            if (games.Count == 0)
            {
                string[] targets = { "League of Legends", "VALORANT" };
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    foreach (var target in targets)
                    {
                        if (games.Any(g => g.Name.Equals(target, StringComparison.OrdinalIgnoreCase))) continue;

                        string[] possiblePaths = {
                            Path.Combine(drive.Name, "Riot Games", target),
                            Path.Combine(drive.Name, "Games", target)
                        };

                        foreach (var path in possiblePaths)
                        {
                            if (Directory.Exists(path))
                                games.Add(new GameEntry { Name = target, InstallLocation = path, Source = "Riot (Disk Scan)" });
                        }
                    }
                }
            }

            return games;
        }

        private static List<GameEntry> GetRegistryGames()
        {
            var games = new List<GameEntry>();
            var uninstallPaths = new[] {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var path in uninstallPaths)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key == null) continue;
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey == null) continue;
                                var name = subKey.GetValue("DisplayName") as string;
                                var installLoc = subKey.GetValue("InstallLocation") as string;

                                if (string.IsNullOrEmpty(installLoc))
                                {
                                    var uninstallString = subKey.GetValue("UninstallString") as string;
                                    if (!string.IsNullOrEmpty(uninstallString) && uninstallString.Contains(".exe"))
                                    {
                                        try { installLoc = Path.GetDirectoryName(uninstallString.Replace("\"", "")); } catch { }
                                    }
                                }

                                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(installLoc) && Directory.Exists(installLoc))
                                {
                                    if (IsRelevantGame(name))
                                    {
                                        games.Add(new GameEntry { Name = name, InstallLocation = installLoc, Source = "Windows Registry" });
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            return games;
        }

        private static bool IsRelevantGame(string name)
        {
            var targets = new[] { "Overwatch", "Call of Duty", "Warzone", "Counter-Strike", "Valorant", "League of Legends", "Dota 2", "Dead by Daylight", "Rocket League", "Rainbow Six" };
            return targets.Any(t => name.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        private static List<GameEntry> GetBattleNetGames()
        {
            var games = new List<GameEntry>();
            var targets = new Dictionary<string, string> { { "Overwatch", "Overwatch" }, { "Call of Duty", "Warzone" } };

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                string[] roots = {
                    drive.Name,
                    Path.Combine(drive.Name, "Program Files (x86)"),
                    Path.Combine(drive.Name, "Program Files"),
                    Path.Combine(drive.Name, "Games"),
                    Path.Combine(drive.Name, "Jogos"),
                    Path.Combine(drive.Name, "Battle.net Games")
                };
                foreach (var root in roots)
                {
                    if (!Directory.Exists(root)) continue;
                    foreach (var t in targets)
                    {
                        string path = Path.Combine(root, t.Key);
                        if (Directory.Exists(path)) games.Add(new GameEntry { Name = t.Value, InstallLocation = path, Source = "Battle.net (Scan)" });
                    }
                }
            }
            return games;
        }

        public static async Task<string> GetGameCoverUrl(string gameName)
        {
            try
            {
                string cleanName = gameName.Replace("®", "").Replace("™", "").Trim();
                string safeFileName = string.Join("_", cleanName.Split(Path.GetInvalidFileNameChars()));
                string localFile = Path.Combine(CachePath, $"{safeFileName}.jpg");
                if (File.Exists(localFile)) return new Uri(localFile).AbsoluteUri;

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
                var response = await client.GetAsync($"https://www.steamgriddb.com/api/v2/search/autocomplete/{Uri.EscapeDataString(cleanName)}");
                if (!response.IsSuccessStatusCode) return null;

                using var searchDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (searchDoc.RootElement.TryGetProperty("data", out JsonElement dataArray) && dataArray.GetArrayLength() > 0)
                {
                    int id = dataArray[0].GetProperty("id").GetInt32();
                    var gridRes = await client.GetStringAsync($"https://www.steamgriddb.com/api/v2/grids/game/{id}?dimensions=600x900");
                    using var gridDoc = JsonDocument.Parse(gridRes);
                    if (gridDoc.RootElement.TryGetProperty("data", out JsonElement grids) && grids.GetArrayLength() > 0)
                    {
                        var imgData = await client.GetByteArrayAsync(grids[0].GetProperty("url").GetString());
                        await File.WriteAllBytesAsync(localFile, imgData);
                        return new Uri(localFile).AbsoluteUri;
                    }
                }
            }
            catch { }
            return null;
        }

        private static List<GameEntry> GetSteamGames()
        {
            var games = new List<GameEntry>();
            string steamPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null);
            if (string.IsNullOrEmpty(steamPath)) return games;
            try
            {
                string libraryVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(libraryVdf)) return games;
                var libraries = new List<string> { steamPath };
                foreach (var line in File.ReadAllLines(libraryVdf))
                    if (line.Contains("\"path\"")) libraries.Add(line.Split('"')[3].Replace(@"\\", @"\"));

                foreach (var lib in libraries)
                {
                    var appPath = Path.Combine(lib, "steamapps");
                    if (!Directory.Exists(appPath)) continue;
                    foreach (var manifest in Directory.GetFiles(appPath, "appmanifest_*.acf"))
                    {
                        var lines = File.ReadAllLines(manifest);
                        var name = lines.FirstOrDefault(l => l.Contains("\"name\""))?.Split('"')[3];
                        var dir = lines.FirstOrDefault(l => l.Contains("\"installdir\""))?.Split('"')[3];
                        if (name != null && dir != null) games.Add(new GameEntry { Name = name, InstallLocation = Path.Combine(appPath, "common", dir), Source = "Steam" });
                    }
                }
            }
            catch { }
            return games;
        }

        private static List<GameEntry> GetEpicGames()
        {
            var games = new List<GameEntry>();
            string manifestDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
            if (!Directory.Exists(manifestDir)) return games;
            foreach (var file in Directory.GetFiles(manifestDir, "*.item"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file));
                    games.Add(new GameEntry { Name = doc.RootElement.GetProperty("DisplayName").GetString(), InstallLocation = doc.RootElement.GetProperty("InstallLocation").GetString(), Source = "Epic" });
                }
                catch { }
            }
            return games;
        }
    }
}
// TESTE GIT