namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;

    [Cmdlet(VerbsLifecycle.Install, "RavenDB", SupportsShouldProcess = true)]
    public class InstallRavenDB : CmdletBase
    {
        [Parameter(HelpMessage = "Port number to be used, Default: 8080")]
        public int Port { get; set; }

        [Parameter(HelpMessage = "Path to install RavenDB into, default is %ProgramFiles%\\NServiceBus.Persistence")]
        public string Path { get; set; }

        protected override void Process()
        {
            if (Port == 0)
            {
                Port = 8080;
            }

            var setup = new Setup.Windows.RavenDB.RavenDBSetup();

            var isGood = setup.Install(null, Port, Path, ShouldProcess(Environment.MachineName));

            WriteObject(isGood);
        }
    }
}
