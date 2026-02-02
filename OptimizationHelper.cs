using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading;
using System.Management; // Nota: Adicione a referência System.Management no seu projeto

namespace FrameHunterFPS
{
    public static class OptimizationHelper
    {
        private static string GuidHighPerf = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        private static string MyPlanGuid = "11111111-1111-1111-1111-111111111111";

        // Verifica se o dispositivo possui bateria (Notebook)
        private static bool IsLaptop()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT BatteryStatus FROM Win32_Battery"))
                {
                    using (var collection = searcher.Get())
                    {
                        return collection.Count > 0;
                    }
                }
            }
            catch { return false; }
        }

        // --- MÉTODO DE UNDO (RESTAURAR PADRÕES DO WINDOWS) ---
        public static void RestoreAllDefaults()
        {
            try
            {
                bool laptop = IsLaptop();

                // 1. Ativa o Plano Equilibrado e remove o customizado
                RunCommand("powercfg", "-setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                RunCommand("powercfg", $"-delete {MyPlanGuid}");

                // 2. Reativa CPU Parking e Timeouts Padrão
                RunCommand("powercfg", "-setacvalueindex scheme_current sub_processor 0cc5b647-c1df-4637-891a-dec35c318583 5");
                RunCommand("powercfg", "-change -monitor-timeout-ac 10");
                RunCommand("powercfg", "-change -disk-timeout-ac 20");

                // Se for notebook, restaura timeouts de bateria para economizar energia
                if (laptop)
                {
                    RunCommand("powercfg", "-setdcvalueindex scheme_current sub_processor 0cc5b647-c1df-4637-891a-dec35c318583 10");
                    RunCommand("powercfg", "-change -monitor-timeout-dc 5");
                    RunCommand("powercfg", "-change -standby-timeout-dc 15");
                }

                RunCommand("powercfg", "-setactive scheme_current");

                // 3. Reativa VBS, HVCI e Fast Startup
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1);
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 1);
                RunCommand("powercfg", "-h on");
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 1);

                // 4. Restaura Visual FX (Deixa o Windows decidir o melhor)
                SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 0);

                // 5. Reativa Telemetria e Indexação
                SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1);
                RunCommand("sc", "config WSearch start= auto");
                RunCommand("sc", "config DiagTrack start= auto");

                // 6. Mouse Stock (Restaura precisão do ponteiro)
                SetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", "1", true);
                SetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold1", "6", true);
                SetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold2", "10", true);

                // Atualiza o sistema instantaneamente
                RunCommand("rundll32.exe", "user32.dll,UpdatePerUserSystemParameters");
            }
            catch { }
        }

        public static void ApplyUltimatePerformance()
        {
            try
            {
                RunCommand("powercfg", $"-delete {MyPlanGuid}");
                Thread.Sleep(200);
                RunCommand("powercfg", $"-duplicatescheme {GuidHighPerf} {MyPlanGuid}");
                Thread.Sleep(200);
                RunCommand("powercfg", $"-setactive {MyPlanGuid}");
                RunCommand("powercfg", $"-changename {MyPlanGuid} \"FrameHunter Ultimate Mode\" \"Optimized for Maximum FPS.\"");
                RunCommand("powercfg", "-change -monitor-timeout-ac 0");
                RunCommand("powercfg", "-change -disk-timeout-ac 0");
                RunCommand("powercfg", "-change -standby-timeout-ac 0");
            }
            catch { }
        }

        public static void ApplyCpuUnpark(bool enable)
        {
            int value = enable ? 100 : 5;
            RunCommand("powercfg", $"-setacvalueindex scheme_current sub_processor 0cc5b647-c1df-4637-891a-dec35c318583 {value}");
            RunCommand("powercfg", "-setactive scheme_current");
        }

        public static void ApplyGameMode(bool enable)
        {
            int val = enable ? 1 : 0;
            SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AutoGameModeEnabled", val);
            SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AllowAutoGameMode", val);
        }

        public static void ApplyHAGS(bool enable)
        {
            int val = enable ? 2 : 1;
            SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", val);
        }

        public static void ApplyVBS(bool disable)
        {
            int val = disable ? 0 : 1;
            SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", val);
            SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", val);
        }

        public static void ApplyFastStartup(bool disable)
        {
            int val = disable ? 0 : 1;
            SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", val);
        }

        public static void ApplyHibernation(bool disable)
        {
            if (disable) RunCommand("powercfg", "-h off");
            else RunCommand("powercfg", "-h on");
        }

        public static void ApplyTelemetry(bool disable)
        {
            int val = disable ? 0 : 1;
            SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", val);
        }

        public static void ApplyBloatwareRemover(bool enable)
        {
            if (enable) SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
        }

        public static void ApplyTransparency(bool disable)
        {
            int val = disable ? 0 : 1;
            SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", val);
        }

        public static void ApplyGameBar(bool disable)
        {
            int val = disable ? 0 : 1;
            SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", val);
            SetRegistryValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", val);
        }

        public static void ApplyTimerResolution(bool optimize)
        {
            if (optimize)
            {
                RunCommand("bcdedit", "/deletevalue useplatformclock");
                RunCommand("bcdedit", "/set disabledynamictick yes");
            }
        }

        public static void ApplyMouseAccel(bool fix)
        {
            string val = fix ? "0" : "1";
            SetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", val, true);
        }

        public static void ApplyUSBPower(bool disable)
        {
            int val = disable ? 1 : 0;
            SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USB", "DisableSelectiveSuspend", val);
        }

        public static void ApplyVisualFX(bool optimize)
        {
            try
            {
                string visualKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
                string desktopKey = @"HKEY_CURRENT_USER\Control Panel\Desktop";
                if (optimize)
                {
                    SetRegistryValue(visualKey, "VisualFXSetting", 3);
                    byte[] mask = { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 };
                    SetRegistryValue(desktopKey, "UserPreferencesMask", mask);
                    SetRegistryValue(desktopKey, "DragFullWindows", "1", true);
                    SetRegistryValue(desktopKey, "FontSmoothing", "2", true);
                }
                else
                {
                    SetRegistryValue(visualKey, "VisualFXSetting", 0);
                }
                RunCommand("rundll32.exe", "user32.dll,UpdatePerUserSystemParameters");
            }
            catch { }
        }

        private static void SetRegistryValue(string keyPath, string valueName, object value, bool isString = false)
        {
            try
            {
                RegistryKey baseKey = keyPath.StartsWith("HKEY_LOCAL_MACHINE") ?
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64) :
                    RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                string subKeyPath = keyPath.Contains(@"\") ? keyPath.Substring(keyPath.IndexOf('\\') + 1) : keyPath;
                using (var key = baseKey.CreateSubKey(subKeyPath))
                {
                    if (key != null)
                    {
                        if (value is byte[] bytes) key.SetValue(valueName, bytes, RegistryValueKind.Binary);
                        else if (isString) key.SetValue(valueName, value?.ToString() ?? "");
                        else key.SetValue(valueName, Convert.ToInt32(value), RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private static void RunCommand(string cmd, string args)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo { FileName = cmd, Arguments = args, CreateNoWindow = true, UseShellExecute = false, Verb = "runas" };
                using (Process proc = Process.Start(info)) proc?.WaitForExit();
            }
            catch { }
        }
    }
}