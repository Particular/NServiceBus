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

    public class RavenDBInstaller : INeedToInstallSomething<Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            if (!Configure.Instance.InstallRavenDB())
                return;

            var installPath = Configure.Instance.RavenInstallPath();

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
    }
}