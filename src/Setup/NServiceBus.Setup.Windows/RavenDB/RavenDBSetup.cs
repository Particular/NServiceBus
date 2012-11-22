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

            var result = QueryHttpForRavenOn(port);

            return result;
        }

        private static bool QueryHttpForRavenOn(int port)
        {
            var webRequest = WebRequest.Create(String.Format("http://localhost:{0}", port));
            webRequest.Timeout = 2000;

            try
            {
                var webResponse = webRequest.GetResponse();
                var serverBuild = webResponse.Headers["Raven-Server-Build"];

                return serverBuild != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Install(int port = 0, string installPath = null)
        {
            if (string.IsNullOrEmpty(installPath))
                installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), DefaultDirectoryName);

            if (Directory.Exists(installPath))
            {
                Console.Out.WriteLine("Path '{0}' already exists please update RavenDB manually if needed.", installPath);
                return;
            }

            var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "RavenDB");
            if(service != null)
            {
                Console.Out.WriteLine("There is already a RavenDB service installed on this computer, the RavenDB service status is {0}.", service.Status);
                return;
            }

            int availablePort;

            //Check if the port is available, if so let the installer setup raven if its being run
            if (port > 0)
            {
                availablePort = FindPort(port);
                if (availablePort != port)
                {
                    Console.Out.WriteLine(
                        "Port '{0}' isn't available, please specify a different port and rerun the command.", port);
                    return;
                }
            }
            else
            {
                availablePort = FindPort(DefaultPort);
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
                    case "Raven.Studio":
                        fileName += ".xap";
                        break;

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
            
            Console.WriteLine("Installing RavenDB as a windows service.");

            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = installPath,
                Arguments = "/install",
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
            
            Console.WriteLine("RavenDB service started.");

            SavePortToBeUsedForRavenInRegistry(availablePort);

            return;
        }

        private static void SavePortToBeUsedForRavenInRegistry(int availablePort)
        {
            using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\NServiceBus"))
            {
                key.SetValue("RavenPort", availablePort, RegistryValueKind.DWord);
            }
        }

        private static int FindPort(int startPort)
        {
            var activeTcpListeners = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners();

            for (var port = startPort; port < startPort + 1024; port++)
            {
                var portCopy = port;
                if (activeTcpListeners.All(endPoint => endPoint.Port != portCopy))
                {
                    return port;
                }
            }

            return startPort;
        }
        
        const string DefaultDirectoryName = "NServiceBus.Persistence";
    }
}