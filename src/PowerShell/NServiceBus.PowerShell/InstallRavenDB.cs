namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using Setup.Windows.RavenDB;

    [Cmdlet(VerbsLifecycle.Install, "NServiceBusRavenDB")]
    public class InstallRavenDB : CmdletBase
    {
        [Parameter(HelpMessage = "Port number to be used, default is 8080", ValueFromPipelineByPropertyName = true)]
        public int Port { get; set; }

        [Parameter(HelpMessage = "Path to install RavenDB into, default is %ProgramFiles%\\NServiceBus.Persistence", ValueFromPipelineByPropertyName = true)]
        public string Path { get; set; }

        protected override void Process()
        {
            RavenDBSetup.Install(Port, Path);
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "NServiceBusRavenDBInstallation")]
    public class ValidateRavenDB : CmdletBase
    {
        [Parameter(HelpMessage = "Port number to be used, default is 8080", ValueFromPipelineByPropertyName = true)]
        public int Port { get; set; }

        protected override void Process()
        {
            var isGood = RavenDBSetup.Check(Port);

            WriteObject(isGood);
        }
    }
}
