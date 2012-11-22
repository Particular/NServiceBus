namespace NServiceBus.PowerShell
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Reflection;
    using Microsoft.Win32;

    [Cmdlet(VerbsLifecycle.Install, "NServiceBusLicense")]
    public class InstallLicense : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "License file path")]
        public string Path { get; set; }

        [Parameter(Mandatory = false, HelpMessage = @"Installs license in HKEY_LOCAL_MACHINE\SOFTWARE\NServiceBus, by default if not specified the license is installed in HKEY_CURRENT_USER\SOFTWARE\NServiceBus")]
        public bool UseHKLM { get; set; }

        protected override void ProcessRecord()
        {
            string selectedLicenseText = ReadAllTextWithoutLocking(Path);

            var rootKey = Registry.CurrentUser;
            if (UseHKLM)
            {
                rootKey = Registry.LocalMachine;
            }

            using (var registryKey = rootKey.CreateSubKey(String.Format(@"SOFTWARE\NServiceBus\{0}", GetNServiceBusVersion().ToString(2))))
            {
                if (registryKey == null)
                {
                    Host.UI.WriteErrorLine("License file could not be installed.");
                    return;
                }
                registryKey.SetValue("License", selectedLicenseText, RegistryValueKind.String);
            }
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