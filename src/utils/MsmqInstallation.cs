using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace NServiceBus.Utils
{
    public static class MsmqInstallation
    {
        public static bool IsMsmqInstalled()
        {
            var dll = LoadLibraryW("Mqrt.dll");
            if (dll != IntPtr.Zero)
                return true;

            return false;
        }

        public static void Install()
        {
            if (IsMsmqInstalled())
                return;

            Console.WriteLine("MSMQ is not installed. Going to install.");

            var osvi = new OSVersionInfoEx();
            osvi.OSVersionInfoSize = (UInt32)Marshal.SizeOf(typeof(OSVersionInfoEx));

            GetVersionEx(osvi);

            Process process = null;
            Action cleanup = null;
            switch(Environment.OSVersion.Version.Major)
            {
                /*Vista or Server 2008*/ case 6 : 

                if (osvi.ProductType == VER_NT_WORKSTATION) // Vista
                    process = Process.Start("OCSETUP", "MSMQ-Container;MSMQ-Server");
                else // Server 2008
                    process = Process.Start("OCSETUP", "MSMQ-Server");

                break;

                /*XP or Server 2003*/ case 5:

                    Console.WriteLine("Handling Windows XP and Server 2003.");

                    var p = Path.GetTempFileName();

                    using (var sw = File.CreateText(p))
                    {
                        foreach(var s in XP_Install)
                            sw.WriteLine(s);

                        sw.Flush();
                    }

                    Console.WriteLine("Executing: sysocmgr.exe /i:sysoc.inf /u:%temp%\\" + Path.GetFileName(p));
                    process = Process.Start("sysocmgr.exe", "/i:sysoc.inf /u:%temp%\\" + Path.GetFileName(p));
                    cleanup = () => File.Delete(p);

                    break;
            }

            if (process != null)
            {
                Console.WriteLine("Waiting for MSMQ setup to complete.");

                process.WaitForExit();

                if (cleanup != null)
                    cleanup();
            }

            Console.WriteLine("Done.");

        }

        /// Return Type: HMODULE->HINSTANCE->HINSTANCE__*
        ///lpLibFileName: LPCWSTR->WCHAR*
        [DllImportAttribute("kernel32.dll", EntryPoint = "LoadLibraryW")]
        public static extern IntPtr LoadLibraryW([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string lpLibFileName);


        [DllImport("Kernel32", CharSet=CharSet.Auto)]
        static extern Boolean GetVersionEx([Out][In]OSVersionInfo versionInformation);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OSVersionInfoEx : OSVersionInfo
        {
            public UInt16 ServicePackMajor;
            public UInt16 ServicePackMinor;
            public UInt16 SuiteMask;
            public byte ProductType;
            public byte Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OSVersionInfo
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

        public const byte VER_NT_WORKSTATION = 1;
        public const byte VER_NT_SERVER = 3;

        public static readonly List<string> XP_Install = new List<string>(new[]
                                                                       {
                                                                           "[Components]",
                                                                           "msmq_Core = ON",
                                                                           "msmq_LocalStorage = ON",
                                                                           "msmq_ADIntegrated = OFF",
                                                                           "msmq_TriggersService = OFF",
                                                                           "msmq_HTTPSupport = OFF",
                                                                           "msmq_RoutingSupport = OFF",
                                                                           "msmq_MQDSService = OFF"
                                                                       });
    }
}
