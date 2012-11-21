namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.RavenDB;

    [Cmdlet(VerbsLifecycle.Install, "RavenDB", SupportsShouldProcess = true)]
    public class InstallRavenDB : CmdletBase
    {
        [Parameter(HelpMessage = "Port number to be used, default is 8080")]
        public int Port { get; set; }

        [Parameter(HelpMessage = "Path to install RavenDB into, default is %ProgramFiles%\\NServiceBus.Persistence")]
        public string Path { get; set; }

        protected override void Process()
        {
            var setup = new RavenDBSetup();

            var isGood = setup.Install(null, Port, Path, ShouldProcess(Environment.MachineName));

            WriteObject(isGood);
        }
    }
}
