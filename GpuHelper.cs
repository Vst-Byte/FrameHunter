using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace FrameHunterFPS
{
    public static class GpuHelper
    {
        // =========================================================
        // 1. DETECÇÃO DE HARDWARE
        // =========================================================
        public static string DetectGpuBrand()
        {
            try
            {
                // Busca no WMI o nome da placa de vídeo
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Name"].ToString().ToLower();
                        if (name.Contains("nvidia")) return "NVIDIA";
                        if (name.Contains("amd") || name.Contains("radeon")) return "AMD";
                    }
                }
            }
            catch { }
            return "UNKNOWN";
        }

        // =========================================================
        // 2. OTIMIZAÇÕES NVIDIA BÁSICAS
        // =========================================================
        public static bool ApplyNvidiaProfile()
        {
            try
            {
                // Caminho: Pasta do App/Resources/
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Path.Combine(basePath, "Resources", "nvidiaProfileInspector.exe");
                string nipPath = Path.Combine(basePath, "Resources", "FrameHunterProfile.nip");

                if (!File.Exists(exePath) || !File.Exists(nipPath)) return false;

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = exePath,
                    // AQUI ESTÁ A MUDANÇA: Adicionado " -silent" para esconder o popup feio
                    Arguments = $"\"{nipPath}\" -silent",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using (var proc = Process.Start(info))
                {
                    proc.WaitForExit();
                }
                return true;
            }
            catch { return false; }
        }

        public static void ApplyNvidiaTweaks()
        {
            // Força o modo de energia "Prefer Maximum Performance" no driver
            SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\Global\NVTweak", "NvCplPowerPolicies", 1);
        }

        // =========================================================
        // 3. OTIMIZAÇÕES AMD
        // =========================================================
        public static void DisableAmdUlps()
        {
            string baseKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

            using (var root = Registry.LocalMachine.OpenSubKey(baseKey, true))
            {
                if (root == null) return;

                foreach (string subKeyName in root.GetSubKeyNames())
                {
                    using (var subKey = root.OpenSubKey(subKeyName, true))
                    {
                        if (subKey == null) continue;

                        if (subKey.GetValue("EnableUlps") != null)
                        {
                            subKey.SetValue("EnableUlps", 0, RegistryValueKind.DWord);
                        }
                    }
                }
            }
        }

        public static void ResetAmdShaderCache()
        {
            try
            {
                string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string amdCache = Path.Combine(localData, "AMD", "DxCache");

                if (Directory.Exists(amdCache))
                {
                    foreach (var file in Directory.GetFiles(amdCache))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch { }
        }

        public static void DisableAmdMPO()
        {
            string key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Dwm";
            try
            {
                Registry.SetValue(key, "OverlayTestMode", 5, RegistryValueKind.DWord);
            }
            catch { }
        }

        public static void DisableAmdDeepSleep()
        {
            string baseKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

            using (var root = Registry.LocalMachine.OpenSubKey(baseKey, true))
            {
                if (root == null) return;

                foreach (string subKeyName in root.GetSubKeyNames())
                {
                    using (var subKey = root.OpenSubKey(subKeyName, true))
                    {
                        if (subKey == null) continue;
                        try
                        {
                            subKey.SetValue("PP_SclkDeepSleepDisable", 1, RegistryValueKind.DWord);
                        }
                        catch { }
                    }
                }
            }
        }

        // =========================================================
        // 5. OTIMIZAÇÕES NVIDIA AVANÇADAS (NOVAS)
        // =========================================================

        /// <summary>
        /// Desativa serviços de telemetria da Nvidia para economizar CPU.
        /// </summary>
        public static void DisableNvidiaTelemetry()
        {
            // 1. Registro
            string driverKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm";
            SetRegistryValue(driverKey, "EnableTelemetry", 0);

            // 2. Serviços (CMD commands)
            string[] services = { "NvTelemetryContainer", "NvContainerLocalSystem", "NvContainerNetworkService" };
            foreach (var service in services)
            {
                try
                {
                    // Desabilita Startup
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"config \"{service}\" start= disabled",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Verb = "runas"
                    });

                    // Para o serviço agora
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"stop \"{service}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Verb = "runas"
                    });
                }
                catch { }
            }
        }

        /// <summary>
        /// Habilita MSI (Message Signaled Interrupts) para reduzir latência.
        /// </summary>
        public static void EnableNvidiaMSI()
        {
            string pciBase = @"SYSTEM\CurrentControlSet\Enum\PCI";

            using (var pciRoot = Registry.LocalMachine.OpenSubKey(pciBase, true))
            {
                if (pciRoot == null) return;

                foreach (string deviceKeyName in pciRoot.GetSubKeyNames())
                {
                    // Verifica se é Nvidia (Vendor ID 10DE)
                    if (deviceKeyName.ToUpper().Contains("VEN_10DE"))
                    {
                        using (var deviceKey = pciRoot.OpenSubKey(deviceKeyName, true))
                        {
                            if (deviceKey == null) continue;

                            foreach (string instanceName in deviceKey.GetSubKeyNames())
                            {
                                string msiPath = $@"{pciBase}\{deviceKeyName}\{instanceName}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";

                                try
                                {
                                    // Cria a chave se não existir
                                    using (var msiKey = Registry.LocalMachine.CreateSubKey(msiPath))
                                    {
                                        if (msiKey != null)
                                        {
                                            msiKey.SetValue("MSISupported", 1, RegistryValueKind.DWord);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
        }

        // =========================================================
        // 4. OTIMIZAÇÕES UNIVERSAIS
        // =========================================================
        public static void SetGpuPriority(bool highPriority)
        {
            int val = highPriority ? 8 : 2;
            string key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";

            SetRegistryValue(key, "GPU Priority", val);
            SetRegistryValue(key, "Priority", highPriority ? 6 : 2);
            SetRegistryValue(key, "Scheduling Category", "High");
        }

        // Função Auxiliar
        private static void SetRegistryValue(string keyPath, string valueName, object value)
        {
            try
            {
                if (keyPath.StartsWith("HKEY_LOCAL_MACHINE"))
                    Registry.SetValue(keyPath, valueName, value);
            }
            catch { }
        }
    }
}