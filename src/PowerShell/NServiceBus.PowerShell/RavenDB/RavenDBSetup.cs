namespace NServiceBus.Setup.Windows.RavenDB
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.ServiceProcess;
    using System.Xml;
    using Microsoft.Win32;
    using Persistence.Raven.Installation;
    using PowerShell;
    using PowerShell.RavenDB;

    public class RavenDBSetup
    {
        public const int DefaultPort = 8080;

        public static bool Check(int port = 0)
        {
            if (port == 0)
            {
                port = DefaultPort;
            }

            var url = String.Format("http://localhost:{0}", port);

            Console.Out.WriteLine("Checking if Raven is listening on {0}.", url);

            var result = IsRavenDBv2RunningOn(port);

            return result;
        }

        public static int FindRavenDBPort()
        {
            var port = ReadRavenPortFromRegistry();

            if (port == 0)
                port = DefaultPort;

            if (Check(port))
                return port;

            return 0;
        }


        private static bool IsRavenDBv2RunningOn(int port)
        {
            var webRequest = WebRequest.Create(String.Format("http://localhost:{0}", port));
            webRequest.Timeout = 2000;

            try
            {
                var webResponse = webRequest.GetResponse();
                var serverBuildHeader = webResponse.Headers["Raven-Server-Build"];

                if (serverBuildHeader == null)
                    return false;

                int serverBuild = 0;

                if (!int.TryParse(serverBuildHeader, out serverBuild))
                    return false;

                return serverBuild >= 2000;//at least raven v2
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Install(int port = 0, string installPath = null)
        {
            if (string.IsNullOrEmpty(installPath))
                installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Replace(" (x86)", String.Empty), DefaultDirectoryName);

            if (Directory.Exists(installPath))
            {
                Console.Out.WriteLine("Path '{0}' already exists please update RavenDB manually if needed.", installPath);
                return;
            }

            var serviceName = "RavenDB";

            var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
            if(service != null)
            {
                Console.Out.WriteLine("There is already a RavenDB service installed on this computer, the RavenDB service status is {0}.", service.Status);

                if (IsRavenDBv2RunningOn(8080)) //todo: we can improve this in the future by looking into the config file to try to figure out the port
                {
                    Console.Out.WriteLine("Existing Raven is v2, NServiceBus will be configured to use it");

                    SavePortToBeUsedForRavenInRegistry(8080);

                    return;
                }

                serviceName = serviceName + "-v2";

            }

            int availablePort;

            //Check if the port is available, if so let the installer setup raven if its being run
            if (port > 0)
            {
                availablePort = PortUtils.FindAvailablePort(port);
                if (availablePort != port)
                {
                    if (IsRavenDBv2RunningOn(port))
                    {
                        Console.Out.WriteLine("A compatible(v2) version of Raven has been found on port {0} and will be used", port);

                        SavePortToBeUsedForRavenInRegistry(port);
                    }
                    else
                    {
                        Console.Out.WriteLine("Port '{0}' isn't available, please specify a different port and rerun the command.", port);    
                    }
                    
                    return;
                }
            }
            else
            {
                availablePort = PortUtils.FindAvailablePort(DefaultPort);

                //is there already a Raven2 on the default port?
                if (availablePort != DefaultPort && IsRavenDBv2RunningOn(DefaultPort))
                {
                    Console.Out.WriteLine("A compatible(v2) version of Raven has been found on port {0} and will be used", DefaultPort);

                    SavePortToBeUsedForRavenInRegistry(DefaultPort);
                    return;
                }
            }
         
            if (!RavenHelpers.EnsureCanListenToWhenInNonAdminContext(availablePort))
            {
                Console.WriteLine("Failed to grant rights for listening to http on port {0}, please specify a different port and rerun the command.", availablePort);
                return;
            }

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            Console.WriteLine("Unpacking resources...");
            string ravenConfigData = null;

            foreach (DictionaryEntry entry in RavenServer.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true))
            {
                var fileName = entry.Key.ToString().Replace("_", ".");

                var data = entry.Value as byte[];
             
                switch (fileName)
                {
                    case "Raven.Server":
                        fileName += ".exe";
                        break;

                    case "Raven.Server.exe":
                        fileName += ".config";
                        ravenConfigData = (string)entry.Value;
                        data = null;
                        break;

                    default:
                        fileName += ".dll";
                        break;
                }

                var destinationPath = Path.Combine(installPath, fileName);

                if(data == null)
                    continue;

                Console.WriteLine("Unpacking '{0}' to '{1}'...", entry.Key, destinationPath);

                File.WriteAllBytes(destinationPath, data);
            }
            
            var ravenConfigPath = Path.Combine(installPath, "Raven.Server.exe.config");
            var ravenConfig = new XmlDocument();

            ravenConfig.LoadXml(ravenConfigData);

            var key = (XmlElement) ravenConfig.DocumentElement.SelectSingleNode("//add[@key='Raven/Port']");

            key.SetAttribute("value", availablePort.ToString(CultureInfo.InvariantCulture));

            ravenConfig.Save(ravenConfigPath);
            Console.Out.WriteLine("Updated Raven configuration to use port {0}.", availablePort);

            SavePortToBeUsedForRavenInRegistry(availablePort);
            
            Console.WriteLine("Installing RavenDB as a windows service.");

            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = installPath,
                Arguments = "--install --service-name=" + serviceName,
                FileName = Path.Combine(installPath, "Raven.Server.exe")
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit(20000);

                var line = process.StandardOutput.ReadToEnd();
                Console.WriteLine(line);

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("The RavenDB service failed to start, Raven.Server.exe exit code was {0}.", process.ExitCode);
                    return;
                }
            }
            
            Console.WriteLine("{0} service started, listening on port: {1}",serviceName,availablePort);
        }

        private static void SavePortToBeUsedForRavenInRegistry(int availablePort)
        {
            if (Environment.Is64BitOperatingSystem)
            {
                WriteRegistry(availablePort, RegistryView.Registry32);

                WriteRegistry(availablePort, RegistryView.Registry64);
            }
            else
            {
                WriteRegistry(availablePort, RegistryView.Default);
            }
        }

        static int ReadRavenPortFromRegistry()
        {
            object portValue = null;

            if (Environment.Is64BitOperatingSystem)
            {
                portValue = ReadRegistry(RegistryView.Registry32) ?? ReadRegistry(RegistryView.Registry64);
            }
            else
            {
                portValue = ReadRegistry(RegistryView.Default);
            }

            if (portValue == null)
                return 0;

            return (int)portValue;
        }

        static void WriteRegistry(int availablePort, RegistryView view)
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).CreateSubKey(@"SOFTWARE\ParticularSoftware\ServiceBus"))
            {
                key.SetValue("RavenPort", availablePort, RegistryValueKind.DWord);
            }
        }

        static object ReadRegistry(RegistryView view)
        {
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).CreateSubKey(@"SOFTWARE\ParticularSoftware\ServiceBus"))
            {
                return key.GetValue("RavenPort");
            }
        }
        
        const string DefaultDirectoryName = "NServiceBus.Persistence.v4";
    }
}