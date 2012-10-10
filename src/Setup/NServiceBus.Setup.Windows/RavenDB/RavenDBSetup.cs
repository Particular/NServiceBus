namespace NServiceBus.Setup.Windows.RavenDB
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using System.ServiceProcess;
    using System.Xml;
    using Persistence.Raven.Installation;

    public class RavenDBSetup
    {
        public bool Install(WindowsIdentity identity, int port = 0, string installPath = null,bool allowInstall = false)
        {
            if (string.IsNullOrEmpty(installPath))
                installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), DefaultDirectoryName);

            if (Directory.Exists(installPath))
            {
                Console.Out.WriteLine("Path {0} already exists please update RavenDB manually if needed",installPath);
                return true;
            }

            var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "RavenDB");
            if(service != null)
            {
                Console.Out.WriteLine("There is already a RavenDB service installed on this computer, current status:{0}",service.Status);
                return true;
            }

            if (!allowInstall)
                return false;

            //Check if the port is available, if so let the installer setup raven if its beeing run
            if (port > 0 && !RavenHelpers.EnsureCanListenToWhenInNonAdminContext(port))
            {
                Console.Out.WriteLine("Port {0} isn't available please choose a different port an rerun the command",port);
                return false;

            }
         
            if (!Directory.Exists(installPath))
                Directory.CreateDirectory(installPath);

            Console.WriteLine("Unpacking resources");
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

                Console.WriteLine("Unpacking {0} to {1}", entry.Key, destinationPath);


                File.WriteAllBytes(destinationPath, data);
            }

            if(port > 0)
            {
                var ravenConfigPath = Path.Combine(installPath, "Raven.Server.exe.config");
                var ravenConfig = new XmlDocument();

                ravenConfig.LoadXml(ravenConfigData);

                var key = (XmlElement) ravenConfig.DocumentElement.SelectSingleNode("//add[@key='Raven/Port']");

                key.SetAttribute("value", port.ToString());

                ravenConfig.Save(ravenConfigPath);
                Console.Out.WriteLine("Updated Raven configuration to use port: {0}",port);
            }

            Console.WriteLine("Installing RavenDB as a windows service");


            var process = Process.Start(new ProcessStartInfo
            {
                Verb = "runas",
                Arguments = "/install",
                FileName = Path.Combine(installPath, "Raven.Server.exe")
            });
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine("The RavenDB service failed to start service, exit code: {0}", process.ExitCode);
                return false;
            }

            Console.WriteLine("RavenDB service started");
            return true;
        }

        const string DefaultDirectoryName = "NServiceBus.Persistence";
    }
}