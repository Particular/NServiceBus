namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.Msmq;

    [Cmdlet(VerbsLifecycle.Install, "NServiceBusMSMQ", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class InstallMsmq : CmdletBase
    {
        [Parameter(Mandatory = false, HelpMessage = "Force reinstall of MSMQ")]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            if (ShouldProcess(Environment.MachineName))
            {
                var msmqIsGood = MsmqSetup.StartMsmqIfNecessary(Force);

                if (!msmqIsGood && !Force)
                {
                    WriteWarning(
                        "Msmq needs to be reinstalled. Please re-run the command with -Force set. NOTE: This will remove all local queues!");
                }
            }
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "NServiceBusMSMQInstallation")]
    public class ValidateMsmq : CmdletBase
    {
        protected override void ProcessRecord()
        {
            var msmqIsGood = MsmqSetup.IsInstallationGood();

            WriteVerbose(msmqIsGood
                             ? "MSMQ is installed and setup for use with NServiceBus."
                             : "MSMQ is not installed.");

            WriteObject(msmqIsGood);
        }
    }
}
