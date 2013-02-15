namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.Msmq;

    [Cmdlet(VerbsLifecycle.Install, "NServiceBusMSMQ", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class InstallMsmq : CmdletBase
    {
        public SwitchParameter Force { get; set; }

        protected override void Process()
        {
            if (ShouldProcess(Environment.MachineName))
            {
                bool msmqIsGood = MsmqSetup.StartMsmqIfNecessary(Force);

                if (!msmqIsGood && !Force)
                {
                    WriteWarning(
                        "Msmq needs to reinstalled, Please rerun the command with -Force set. NOTE: This will remove all local queues!");
                }
            }
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "NServiceBusMSMQInstallation")]
    public class ValidateMsmq : CmdletBase
    {
        protected override void Process()
        {
            var msmqIsGood = MsmqSetup.IsInstallationGood();

            Host.UI.WriteLine(msmqIsGood
                                        ? "MSMQ is installed and setup for use with NServiceBus."
                                        : "MSMQ is not installed.");

            WriteObject(msmqIsGood);
        }
    }
}
