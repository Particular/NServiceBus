namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.Msmq;

    [Cmdlet(VerbsLifecycle.Install, "Msmq", SupportsShouldProcess = true)]
    public class InstallMsmq : CmdletBase
    {
        public SwitchParameter Force { get; set; }

        protected override void Process()
        {
            bool msmqIsGood;
            if (!ShouldProcess(Environment.MachineName))
            {
                msmqIsGood = MsmqSetup.IsInstallationGood();
                Host.UI.WriteLine(msmqIsGood
                                          ? "Msmq is installed and setup for use with NServiceBus"
                                          : "Msmq is not installed");

                WriteObject(msmqIsGood);
                return;
            }

            msmqIsGood = MsmqSetup.StartMsmqIfNecessary(Force);

            if (!msmqIsGood && !Force)
                WriteWarning("Msmq needs to reinstalled, Please rerun the command with -Force set. NOTE: This will remove all local queues!");

            WriteObject(msmqIsGood);
        }
    }
}
