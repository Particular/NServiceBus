namespace NServiceBus.Persistence.Raven.Installation
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security.Principal;
    using System.Text;
    using NServiceBus.Installation;
    using NServiceBus.Installation.Environments;

    public class RavenDBInstaller : INeedToInstallInfrastructure<Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!InstallEnabled)
                return;

            var installPath = RavenInstallPath;

            if (Directory.Exists(installPath))
                return;

            //Check if the port is available, if so let the installer setup raven if its beeing run
            if(!RavenHelpers.EnsureCanListenToWhenInNonAdminContext(RavenPersistenceConstants.DefaultPort))
                return;

            if (!Directory.Exists(installPath))
                Directory.CreateDirectory(installPath);

            Console.WriteLine("Unpacking resources");

            foreach (DictionaryEntry entry in Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true))
            {
                var fileName = entry.Key.ToString().Replace("_", ".");

                byte[] data = entry.Value as byte[];

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
                        data = Encoding.ASCII.GetBytes((string)entry.Value);
                        break;

                    default:
                        fileName += ".dll";
                        break;

                }

                var destinationPath = Path.Combine(installPath, fileName);

                Console.WriteLine(string.Format("Unpacking {0} to {1}", entry.Key, destinationPath));


                File.WriteAllBytes(destinationPath, data);
            }

            Console.WriteLine("Installing service");


            var process = Process.Start(new ProcessStartInfo
            {
                Verb = "runas",
                Arguments = "/install",
                FileName = Path.Combine(installPath, "Raven.Server.exe")
            });
            process.WaitForExit();

            if (process.ExitCode != 0)
                Console.WriteLine("NServiceBus.Persistence failed to start service, exit code:" + process.ExitCode);
            else
                Console.WriteLine("NServiceBus.Persistence service started");
        }

        static RavenDBInstaller()
        {
            InstallEnabled = true;
        }

        public static bool InstallEnabled { get; set; }

        static string ravenInstallPath;
        const string InstallDirectory = "NServiceBus.Persistence";

        public static string RavenInstallPath
        {
            get
            {
                if (string.IsNullOrEmpty(ravenInstallPath))
                    ravenInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), InstallDirectory);

                return ravenInstallPath;
            }
            set { ravenInstallPath = value; }
        }

        
    }
}