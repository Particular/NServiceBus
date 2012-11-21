namespace LicenseInstaller
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Windows.Forms;
    using Microsoft.Win32;

    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            string licensePath = null;

            if (args.Length > 0)
            {
                licensePath = args[0];
            }

            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "License files (*.xml)|*.xml|All files (*.*)|*.*";
                openDialog.Title = "Select License file";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    licensePath = openDialog.FileName;
                }
            }

            if (String.IsNullOrWhiteSpace(licensePath))
            {
                Console.Out.WriteLine("License file not installed.");
                return 1;
            }

            string selectedLicenseText = ReadAllTextWithoutLocking(licensePath);

            using (var registryKey = Registry.CurrentUser.CreateSubKey(String.Format(@"SOFTWARE\NServiceBus\{0}", GetNServiceBusVersion().ToString(2))))
            {
                if (registryKey == null)
                {
                    return 1;
                }
                registryKey.SetValue("License", selectedLicenseText, RegistryValueKind.String);
            }

            Console.Out.WriteLine("License file installed.");

            return 0;
        }

        static string ReadAllTextWithoutLocking(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var textReader = new StreamReader(fileStream))
            {
                return textReader.ReadToEnd();
            }
        }

        static Version GetNServiceBusVersion()
        {
            var assembyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            return new Version(assembyVersion.Major, assembyVersion.Minor);
        }
    }
}
