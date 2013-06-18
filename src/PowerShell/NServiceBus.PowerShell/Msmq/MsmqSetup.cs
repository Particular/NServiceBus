﻿namespace NServiceBus.Setup.Windows.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.ServiceProcess;
    using Microsoft.Win32;

    /// <summary>
    /// Utility class for starting and installing MSMQ.
    /// </summary>
    public static class MsmqSetup
    {
        /// <summary>
        /// Checks that MSMQ is installed, configured correctly, and started, and if not
        /// takes the necessary corrective actions to make it so.
        /// </summary>
        public static bool StartMsmqIfNecessary(bool allowReinstall = false)
        {
            if(!InstallMsmqIfNecessary(allowReinstall))
            {
                return false;
            }

            var controller = new ServiceController { ServiceName = "MSMQ", MachineName = "." };

            if (IsStopped(controller))
            {
                ProcessUtil.ChangeServiceStatus(controller, ServiceControllerStatus.Running, controller.Start);
            }

            return true;
        }

        private static bool IsStopped(ServiceController controller)
        {
            return controller.Status == ServiceControllerStatus.Stopped || controller.Status == ServiceControllerStatus.StopPending;
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

        private static bool InstallMsmqIfNecessary(bool allowReinstall)
        {
            Console.WriteLine("Checking if MSMQ is installed.");

            var os = GetOperatingSystem();
            Func<Process> process = null;

            if (IsMsmqInstalled())
            {
                Console.WriteLine("MSMQ is installed.");
                Console.WriteLine("Checking that only needed components are active.");

                if (IsInstallationGood())
                {
                    Console.WriteLine("Installation is good.");
                    return true;
                }

                Console.WriteLine("Installation isn't good.");

                if (!allowReinstall)
                {
                    return false;
                }

                Console.WriteLine("Going to re-install MSMQ. A reboot may be required.");

                switch (os)
                {
                    case OperatingSystemEnum.XpOrServer2003:
                        process = InstallMsmqOnXpOrServer2003;
                        break;

                    case OperatingSystemEnum.Vista:
                        process = () => Process.Start(OcSetup, OcSetupVistaUninstallCommand);
                        break;
                        
                    case OperatingSystemEnum.Windows7:
                    case OperatingSystemEnum.Windows8:
                    case OperatingSystemEnum.Server2008:
                        process = () => Process.Start(OcSetup, OcSetupUninstallCommand);
                        break;

                    case OperatingSystemEnum.Server2012:
                        process = () => Process.Start(Powershell, PowershellUninstallCommand);
                        break;

                    default:
                        Console.WriteLine("OS not supported.");
                        break;
                }

                Console.WriteLine("Uninstalling MSMQ.");
                RunSetup(process);
            }
            else
            {
                Console.WriteLine("MSMQ is not installed. Going to install.");
            }

            switch (os)
            {
                case OperatingSystemEnum.XpOrServer2003:
                    process = InstallMsmqOnXpOrServer2003;
                    break;

                case OperatingSystemEnum.Vista:
                    process = () => Process.Start(OcSetup, OcSetupVistaInstallCommand);
                    break;

                case OperatingSystemEnum.Windows7:
                case OperatingSystemEnum.Windows8:
                case OperatingSystemEnum.Server2008:
                    process = () => Process.Start(OcSetup, OcSetupInstallCommand);
                    break;

                case OperatingSystemEnum.Server2012:
                    process = () => Process.Start(Powershell, PowershellInstallCommand);
                    break;

                default:
                    Console.WriteLine("OS not supported.");
                    break;
            }

            RunSetup(process);

            Console.WriteLine("Installation of MSMQ successful.");

            return true;
        }

        private static void RunSetup(Func<Process> action)
        {
            using (var process = action())
            {
                if (process == null) return;

                Console.WriteLine("Waiting for process to complete.");
                process.WaitForExit();
            }
        }

        private static Process InstallMsmqOnXpOrServer2003()
        {
            var p = Path.GetTempFileName();

            Console.WriteLine("Creating installation instruction file.");

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

            Console.WriteLine("Installation instruction file created.");
            Console.WriteLine("Invoking MSMQ installation.");

            return Process.Start("sysocmgr", "/i:sysoc.inf /x /q /w /u:%temp%\\" + Path.GetFileName(p));
        }

        // Based on http://msdn.microsoft.com/en-us/library/windows/desktop/ms724833(v=vs.85).aspx
        private static OperatingSystemEnum GetOperatingSystem()
        {
            var osvi = new OSVersionInfoEx {OSVersionInfoSize = (UInt32) Marshal.SizeOf(typeof (OSVersionInfoEx))};

            GetVersionEx(osvi);
            
            switch (Environment.OSVersion.Version.Major)
            {
                case 6:
                    switch (Environment.OSVersion.Version.Minor)
                    {
                        case 0:

                            if (osvi.ProductType == VER_NT_WORKSTATION)
                            {
                                return OperatingSystemEnum.Vista;
                            }

                            return OperatingSystemEnum.Server2008;

                        case 1:
                            if (osvi.ProductType == VER_NT_WORKSTATION)
                            {
                                return OperatingSystemEnum.Windows7;
                            }

                            return OperatingSystemEnum.Server2008;

                        case 2:
                            if (osvi.ProductType == VER_NT_WORKSTATION)
                            {
                                return OperatingSystemEnum.Windows8;
                            }

                            return OperatingSystemEnum.Server2012;
                    }
                    break;

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
                    Console.WriteLine("Undesirable MSMQ component installed: " + i);
                    return false;
                }

                if (UndesirableMsmqComponentsV4.Contains(i))
                {
                    Console.WriteLine("Undesirable MSMQ component installed: " + i);
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


        [DllImport("Kernel32", CharSet = CharSet.Auto)]
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

        enum OperatingSystemEnum { DontCare, XpOrServer2003, Vista, Server2008, Windows7, Windows8, Server2012 }

        const string OcSetup = "OCSETUP";
        const string Powershell = "PowerShell";
        const string OcSetupInstallCommand = "MSMQ-Server /passive";
        const string OcSetupUninstallCommand = "MSMQ-Server /passive /uninstall";
        const string OcSetupVistaInstallCommand = "MSMQ-Container;MSMQ-Server /passive";
        const string OcSetupVistaUninstallCommand = "MSMQ-Container;MSMQ-Server /passive /uninstall";
        const string PowershellInstallCommand = @"-Command ""& {Install-WindowsFeature –Name MSMQ-Server}""";
        const string PowershellUninstallCommand = @"-Command ""& {Uninstall-WindowsFeature –Name MSMQ-Server}""";

    }
}
