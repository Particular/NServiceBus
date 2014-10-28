namespace NServiceBus.Setup.Windows.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.ServiceProcess;
    using System.Text;
    using Microsoft.Win32;

    /// <summary>
    /// Utility class for starting and installing MSMQ.
    /// </summary>
    public static class MsmqSetup
    {
        /// <summary>
        /// Checks that MSMQ is installed, configured correctly, and started, and if not takes the necessary corrective actions to make it so.
        /// </summary>
        public static bool StartMsmqIfNecessary()
        {
            Console.WriteLine("Entering StartMsmqIfNecessary in NServiceBus.Setup.Windows.Msmq.MsmqSetup");

            if (!InstallMsmqIfNecessary())
            {
                return false;
            }

            try
            {
                using (var controller = new ServiceController("MSMQ"))
                {
                    if (IsStopped(controller))
                    {
                        ProcessUtil.ChangeServiceStatus(controller, ServiceControllerStatus.Running, controller.Start);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("MSMQ windows service not found! You may need to reboot after MSMQ has been installed.");
                return false;
            }

            return true;
        }

        private static bool IsStopped(ServiceController controller)
        {
            return controller.Status == ServiceControllerStatus.Stopped || controller.Status == ServiceControllerStatus.StopPending;
        }

        internal static bool IsMsmqInstalled()
        {
            var dll = LoadLibraryW("Mqrt.dll");
            return (dll != IntPtr.Zero);
        }

        /// <summary>
        /// Determines if the msmq installation on the current machine is ok
        /// </summary>
        public static bool IsInstallationGood()
        {
            var msmqSetup = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSMQ\Setup");
            if (msmqSetup == null)
            {
                return false;
            }

            var installedComponents = new List<string>(msmqSetup.GetValueNames());
            msmqSetup.Close();

            return HasOnlyNeededComponents(installedComponents);
        }

        static bool InstallMsmqIfNecessary()
        {
            Console.WriteLine("Checking if MSMQ is installed.");

            var os = GetOperatingSystem();

            if (IsMsmqInstalled())
            {
                Console.WriteLine("MSMQ is installed.");
                Console.WriteLine("Checking that only needed components are active.");

                if (IsInstallationGood())
                {
                    Console.WriteLine("Installation is good.");
                    return true;
                }

                Console.WriteLine(
                    "Installation isn't good. Make sure you remove the following components: {0} and also {1}",
                    String.Join(", ", UndesirableMsmqComponentsXp), String.Join(", ", UndesirableMsmqComponentsV4));

                return false;
            }

            Console.WriteLine("MSMQ is not installed. Going to install.");

            switch (os)
            {
                case OperatingSystemEnum.XpOrServer2003:
                    InstallMsmqOnXpOrServer2003();
                    break;

                case OperatingSystemEnum.Vista:
                    RunExe(OcSetup, OcSetupVistaInstallCommand);
                    break;

                case OperatingSystemEnum.Server2008:
                    RunExe(OcSetup, OcSetupInstallCommand);
                    break;

                case OperatingSystemEnum.Windows7:
                    RunExe(dismPath, @"/Online /NoRestart /English /Enable-Feature /FeatureName:MSMQ-Container /FeatureName:MSMQ-Server");
                    break;
                case OperatingSystemEnum.Windows8:
                case OperatingSystemEnum.Server2012:
                    RunExe(dismPath, @"/Online /NoRestart /English /Enable-Feature /all /FeatureName:MSMQ-Server");
                    break;

                default:
                    Console.WriteLine("OS not supported.");
                    return false;
            }

            Console.WriteLine("Installation of MSMQ successful.");

            return true;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

        public static void RunExe(string filename, string args)
        {
            var startInfo = new ProcessStartInfo(filename, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetTempPath()
            };

            Console.Out.WriteLine("Executing {0} {1}", startInfo.FileName, startInfo.Arguments);

            var ptr = new IntPtr();
            var fileSystemRedirectionDisabled = false;

            if (Environment.Is64BitOperatingSystem)
            {
                fileSystemRedirectionDisabled = Wow64DisableWow64FsRedirection(ref ptr);
            }

            try
            {
                using (var process = new Process())
                {
                    var output = new StringBuilder();
                    var error = new StringBuilder();

                    process.StartInfo = startInfo;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    Console.Out.WriteLine(output.ToString());
                    Console.Out.WriteLine(error.ToString());
                }
            }
            finally
            {
                if (fileSystemRedirectionDisabled)
                {
                    Wow64RevertWow64FsRedirection(ptr);
                }
            }
        }

        static void InstallMsmqOnXpOrServer2003()
        {
            var p = Path.GetTempFileName();

            Console.WriteLine("Creating installation instructions file.");

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

            Console.WriteLine("Installation instructions file created.");
            Console.WriteLine("Invoking MSMQ installation.");

            RunExe("sysocmgr", "/i:sysoc.inf /x /q /w /u:%temp%\\" + Path.GetFileName(p));
        }

        // Based on http://msdn.microsoft.com/en-us/library/windows/desktop/ms724833(v=vs.85).aspx
        static OperatingSystemEnum GetOperatingSystem()
        {
            var osVersionInfoEx = new OSVersionInfoEx { OSVersionInfoSize = (UInt32)Marshal.SizeOf(typeof(OSVersionInfoEx)) };

            GetVersionEx(osVersionInfoEx);

            switch (Environment.OSVersion.Version.Major)
            {
                case 6:
                    switch (Environment.OSVersion.Version.Minor)
                    {
                        case 0:

                            if (osVersionInfoEx.ProductType == VER_NT_WORKSTATION)
                            {
                                return OperatingSystemEnum.Vista;
                            }

                            return OperatingSystemEnum.Server2008;

                        case 1:
                            if (osVersionInfoEx.ProductType == VER_NT_WORKSTATION)
                            {
                                return OperatingSystemEnum.Windows7;
                            }

                            return OperatingSystemEnum.Server2008;

                        case 2:
                            if (osVersionInfoEx.ProductType == VER_NT_WORKSTATION)
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

        static bool HasOnlyNeededComponents(IEnumerable<string> installedComponents)
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
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW")]
        static extern IntPtr LoadLibraryW([In] [MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName);


        [DllImport("Kernel32", CharSet = CharSet.Auto)]
        static extern Boolean GetVersionEx([Out][In]OSVersionInfo versionInformation);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class OSVersionInfoEx : OSVersionInfo
        {
// ReSharper disable UnusedField.Compiler
            public UInt16 ServicePackMajor;
            public UInt16 ServicePackMinor;
            public UInt16 SuiteMask;
// ReSharper disable once UnassignedField.Compiler
            public byte ProductType;
            public byte Reserved;
// ReSharper restore UnusedField.Compiler
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class OSVersionInfo
        {
// ReSharper disable once NotAccessedField.Local
            public UInt32 OSVersionInfoSize =
               (UInt32)Marshal.SizeOf(typeof(OSVersionInfo));
// ReSharper disable UnusedField.Compiler
            public UInt32 MajorVersion = 0;
            public UInt32 MinorVersion = 0;
            public UInt32 BuildNumber = 0;
            public UInt32 PlatformId = 0;
            // Attribute used to indicate marshalling for String field
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public String CSDVersion = null;
 // ReSharper restore UnusedField.Compiler
        }

        const byte VER_NT_WORKSTATION = 1;

        static List<string> RequiredMsmqComponentsXp = new List<string>(new[] { "msmq_Core", "msmq_LocalStorage" });
        static List<string> UndesirableMsmqComponentsXp = new List<string>(new[] { "msmq_ADIntegrated", "msmq_TriggersService", "msmq_HTTPSupport", "msmq_RoutingSupport", "msmq_MQDSService" });
        static List<string> UndesirableMsmqComponentsV4 = new List<string>(new[] { "msmq_DCOMProxy", "msmq_MQDSServiceInstalled", "msmq_MulticastInstalled", "msmq_RoutingInstalled", "msmq_TriggersInstalled" });

        enum OperatingSystemEnum
        {
            DontCare, 
            XpOrServer2003, 
            Vista, 
            Server2008, 
            Windows7,
            Windows8, 
            Server2012
        }

        const string OcSetup = "OCSETUP";
        static string dismPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dism.exe");
        const string OcSetupInstallCommand = "MSMQ-Server /passive";
        const string OcSetupVistaInstallCommand = "MSMQ-Container;MSMQ-Server /passive";
    }
}
