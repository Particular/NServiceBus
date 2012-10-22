namespace NServiceBus.PowerShell
{
    using System.Management.Automation;

    [Cmdlet(VerbsLifecycle.Install, "RavenDB")]
    public class InstallRavenDB : CmdletBase
    {
        [Parameter(HelpMessage = "Port number to be used, Default: 8080")]
        public int Port { get; set; }

        [Parameter(HelpMessage = "Path to install RavenDB into, default is %ProgramFiles%\\NServiceBus.Persistence")]
        public string InstallPath { get; set; }

        [Parameter(HelpMessage = "Checks if the RavenDB needs to be installed")]
        public SwitchParameter WhatIf { get; set; }

        protected override void ProcessRecord()
        {
            if (Port == 0)
                Port = 8080;

            var setup = new Setup.Windows.RavenDB.RavenDBSetup();

            var isGood = setup.Install(null, Port, InstallPath,!WhatIf);

            WriteObject(isGood);
        }
    }
}
