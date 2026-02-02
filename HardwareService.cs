using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Management; // Adicione esta referência no seu projeto
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FrameHunterFPS
{
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    public static class HardwareService
    {
        private static Computer? _computer;

        public static string CpuName { get; private set; } = "Detecting...";
        public static string CpuClock { get; private set; } = "0 MHz";
        public static int CpuLoad { get; private set; }
        public static int GpuTemp { get; private set; }
        public static string GpuName { get; private set; } = "Detecting...";
        public static string GpuClock { get; private set; } = "0 MHz";
        public static int RamLoad { get; private set; }
        public static string RamUsed { get; private set; } = "0";
        public static string RamTotal { get; private set; } = "0";
        public static string RamClock { get; private set; } = "0 MHz";
        public static int PingMs { get; private set; }

        public static void Start()
        {
            if (_computer != null) return;
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true, // Ativado para ajudar no barramento Ryzen
                IsControllerEnabled = true
            };
            try { _computer.Open(); } catch { }
        }

        public static async Task Update()
        {
            if (_computer == null) Start();
            await Task.Run(() =>
            {
                try
                {
                    _computer?.Accept(new UpdateVisitor());
                    foreach (var hw in _computer?.Hardware ?? Enumerable.Empty<IHardware>())
                    {
                        // --- CPU (Ryzen 5 5600) ---
                        if (hw.HardwareType == HardwareType.Cpu)
                        {
                            CpuName = hw.Name.Replace("AMD Ryzen", "Ryzen").Trim();

                            var load = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total"));
                            CpuLoad = (int)(load?.Value ?? 0);

                            // Tenta pegar o Clock via sensor
                            var clocks = hw.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Value > 0).ToList();
                            if (clocks.Any())
                            {
                                CpuClock = $"{(int)clocks.Max(s => s.Value)} MHz";
                            }
                            else
                            {
                                // FALLBACK: Se o sensor falhar, usa WMI para pegar a velocidade
                                CpuClock = GetCpuClockWmi();
                            }
                        }

                        // --- GPU (GTX 1650 Super) ---
                        if (hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd)
                        {
                            GpuName = hw.Name.Replace("NVIDIA GeForce", "").Trim();
                            var gt = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                            GpuTemp = (int)(gt?.Value ?? 0);

                            var gc = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && (s.Name.Contains("Core") || s.Name.Contains("Clock")));
                            GpuClock = $"{(int)(gc?.Value ?? 0)} MHz";
                        }

                        // --- RAM (Velocidade Estável) ---
                        if (hw.HardwareType == HardwareType.Memory)
                        {
                            var rl = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                            RamLoad = (int)(rl?.Value ?? 0);

                            var ru = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Used"));
                            var ra = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Available"));

                            RamUsed = $"{(ru?.Value ?? 0):F1}";
                            RamTotal = $"{((ru?.Value ?? 0) + (ra?.Value ?? 0)):F0} GB";

                            // RAM Clock costuma ser 0 via sensor, usamos WMI
                            if (RamClock == "0 MHz") UpdateRamClockWmi();
                        }
                    }
                    UpdatePing();
                }
                catch { }
            });
        }

        private static string GetCpuClockWmi()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return $"{obj["MaxClockSpeed"]} MHz";
                    }
                }
            }
            catch { }
            return "--- MHz";
        }

        private static void UpdateRamClockWmi()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ConfiguredClockSpeed FROM Win32_PhysicalMemory"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        RamClock = $"{obj["ConfiguredClockSpeed"]} MHz";
                        return;
                    }
                }
            }
            catch { }
        }

        private static void UpdatePing()
        {
            try
            {
                using var ping = new Ping();
                var r = ping.Send("8.8.8.8", 800);
                PingMs = r.Status == IPStatus.Success ? (int)r.RoundtripTime : 0;
            }
            catch { PingMs = 0; }
        }
    }
}