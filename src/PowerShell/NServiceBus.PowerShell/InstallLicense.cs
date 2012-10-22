namespace NServiceBus.PowerShell
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Reflection;
    using Microsoft.Win32;

    [Cmdlet(VerbsLifecycle.Install, "License")]
    public class InstallLicense : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "License file path")]
        public string LicenseFilePath { get; set; }

        protected override void ProcessRecord()
        {
            string selectedLicenseText = ReadAllTextWithoutLocking(LicenseFilePath);

            using (var registryKey = Registry.CurrentUser.CreateSubKey(String.Format(@"SOFTWARE\NServiceBus\{0}", GetNServiceBusVersion().ToString(2))))
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