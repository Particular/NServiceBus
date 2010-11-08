using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.ServiceProcess;
using Microsoft.Win32;
using Common.Logging;

namespace NServiceBus.Utils
{
    /// <summary>
    /// Utility class for starting and installing MSMQ.
    /// </summary>
    public static class MsmqInstallation
    {
        /// <summary>
        /// Checks that MSMQ is installed, configured correctly, and started, and if not
        /// takes the necessary corrective actions to make it so.
        /// </summary>
        public static void StartMsmqIfNecessary()
        {
            InstallMsmqIfNecessary();

            var controller = new ServiceController { ServiceName = "MSMQ", MachineName = "." };

            ProcessUtil.ChangeServiceStatus(controller, ServiceControllerStatus.Running, controller.Start);
        }


        
        private static bool IsMsmqInstalled()
        {
            var dll = LoadLibraryW("Mqrt.dll");
            return (dll != IntPtr.Zero);
        }

        /// <summary>
        /// Determines if the msmq installation on the current machine is ok
        /// </summary>
        /// <returns></returns>
        public static bool IsInstallationGood()
        {
            var msmqSetup = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSMQ\Setup");
            if (msmqSetup == null)
                return false;

            var installedComponents = new List<string>(msmqSetup.GetValueNames());
            msmqSetup.Close();

            return HasOnlyNeededComponents(installedComponents);
        }

        private static void InstallMsmqIfNecessary()
        {
            Logger.Debug("Checking if MSMQ is installed.");
            if (IsMsmqInstalled())
            {
                Logger.Debug("MSMQ is installed.");
                Logger.Debug("Checking that only needed components are active.");

                if (IsInstallationGood())
                {
                    Logger.Debug("Installation is good.");
                    return;
                }

                Logger.Debug("Installation isn't good.");
                Logger.Debug("Going to re-install MSMQ. A reboot may be required.");

                PerformFunctionDependingOnOS(
                    () => Process.Start(OcSetup, VistaOcSetupParams + Uninstall),
                    () => Process.Start(OcSetup, Server2008OcSetupParams + Uninstall),
                    InstallMsmqOnXpOrServer2003
                );

                Logger.Debug("Installation of MSMQ successful.");

                return;
            }

            Logger.Debug("MSMQ is not installed. Going to install.");

            PerformFunctionDependingOnOS(
                () => Process.Start(OcSetup, VistaOcSetupParams),
                () => Process.Start(OcSetup, Server2008OcSetupParams),
                InstallMsmqOnXpOrServer2003
                );
            
            Logger.Debug("Installation of MSMQ successful.");
        }
        
        private static void PerformFunctionDependingOnOS(Func<Process> vistaFunc, Func<Process> server2008Func, Func<Process> xpAndServer2003Func)
        {
            var os = GetOperatingSystem();

            Process process = null;
            switch (os)
            {
                case OperatingSystemEnum.Vista:

                    process = vistaFunc();
                    break;

                case OperatingSystemEnum.Server2008:

                    process = server2008Func();
                    break;

                case OperatingSystemEnum.XpOrServer2003:

                    process = xpAndServer2003Func();
                    break;

                default:

                    Logger.Warn("OS not supported.");
                    break;
            }

            if (process == null) return;

            Logger.Debug("Waiting for process to complete.");
            process.WaitForExit();
        }

        private static Process InstallMsmqOnXpOrServer2003()
        {
            var p = Path.GetTempFileName();

            Logger.Debug("Creating installation instruction file.");

            using (var sw = File.CreateText(p))
            {
                sw.WriteLine("[Version]");
                sw.WriteLine("Signature = \"$Windows NT$\"");
                sw.WriteLine();
                sw.WriteLine("[Global]");
                sw.WriteLine("FreshMode = Custom");
                sw.WriteLine("MaintenanceMode = RemoveAll");
                sw.WriteLine("UpgradeMode = UpgradeOnly");
                sw.WriteLine();
                sw.WriteLine("[Components]");

                foreach (var s in RequiredMsmqComponentsXp)
                    sw.WriteLine(s + " = ON");

                foreach (var s in UndesirableMsmqComponentsXp)
                    sw.WriteLine(s + " = OFF");

                sw.Flush();
            }

            Logger.Debug("Installation instruction file created.");
            Logger.Debug("Invoking MSMQ installation.");

            return Process.Start("sysocmgr", "/i:sysoc.inf /x /q /w /u:%temp%\\" + Path.GetFileName(p));
        }

        private static OperatingSystemEnum GetOperatingSystem()
        {
            var osvi = new OSVersionInfoEx();
            osvi.OSVersionInfoSize = (UInt32)Marshal.SizeOf(typeof(OSVersionInfoEx));

            GetVersionEx(osvi);

            switch (Environment.OSVersion.Version.Major)
            {
                case 6:

                    if (osvi.ProductType == VER_NT_WORKSTATION)
                        return OperatingSystemEnum.Vista;
                    
                    return OperatingSystemEnum.Server2008;

                case 5:
                    return OperatingSystemEnum.XpOrServer2003;
            }

            return OperatingSystemEnum.DontCare;
        }

        private static bool HasOnlyNeededComponents(IEnumerable<string> installedComponents)
        {
            var needed = new List<string>(RequiredMsmqComponentsXp);

            foreach (var i in installedComponents)
            {
                if (UndesirableMsmqComponentsXp.Contains(i))
                {
                    Logger.Warn("Undesirable MSMQ component installed: " + i);
                    return false;
                }

                if (UndesirableMsmqComponentsV4.Contains(i))
                {
                    Logger.Warn("Undesirable MSMQ component installed: " + i);
                    return false;
                }

                needed.Remove(i);
            }

            if (needed.Count == 0)
                return true;

            return false;
        }

        /// Return Type: HMODULE->HINSTANCE->HINSTANCE__*
        ///lpLibFileName: LPCWSTR->WCHAR*
        [DllImportAttribute("kernel32.dll", EntryPoint = "LoadLibraryW")]
        static extern IntPtr LoadLibraryW([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string lpLibFileName);


        [DllImport("Kernel32", CharSet=CharSet.Auto)]
        static extern Boolean GetVersionEx([Out][In]OSVersionInfo versionInformation);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class OSVersionInfoEx : OSVersionInfo
        {
            public UInt16 ServicePackMajor;
            public UInt16 ServicePackMinor;
            public UInt16 SuiteMask;
            public byte ProductType;
            public byte Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class OSVersionInfo
        {
            public UInt32 OSVersionInfoSize =
               (UInt32)Marshal.SizeOf(typeof(OSVersionInfo));
            public UInt32 MajorVersion = 0;
            public UInt32 MinorVersion = 0;
            public UInt32 BuildNumber = 0;
            public UInt32 PlatformId = 0;
            // Attribute used to indicate marshalling for String field
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public String CSDVersion = null;
        }

        const byte VER_NT_WORKSTATION = 1;
        const byte VER_NT_SERVER = 3;

        static readonly List<string> RequiredMsmqComponentsXp = new List<string>(new[] { "msmq_Core", "msmq_LocalStorage" });
        static readonly List<string> UndesirableMsmqComponentsXp = new List<string>(new[] { "msmq_ADIntegrated", "msmq_TriggersService", "msmq_HTTPSupport", "msmq_RoutingSupport", "msmq_MQDSService" });
        static readonly List<string> UndesirableMsmqComponentsV4 = new List<string>(new[] { "msmq_DCOMProxy", "msmq_MQDSServiceInstalled", "msmq_MulticastInstalled", "msmq_RoutingInstalled", "msmq_TriggersInstalled" });

        enum OperatingSystemEnum { DontCare, XpOrServer2003, Vista, Server2008 }

        const string OcSetup = "OCSETUP";
        const string Uninstall = " /uninstall";
        const string Server2008OcSetupParams = "MSMQ-Server /passive";
        const string VistaOcSetupParams = "MSMQ-Container;" + Server2008OcSetupParams;

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Utils");
    }
}
