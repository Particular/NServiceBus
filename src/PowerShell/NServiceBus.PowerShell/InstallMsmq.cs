namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.Msmq;

    [Cmdlet(VerbsLifecycle.Install, "NServiceBusMSMQ", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class InstallMsmq : CmdletBase
    {
        protected override void ProcessRecord()
        {
            if (ShouldProcess(Environment.MachineName))
            {
                var msmqIsGood = MsmqSetup.StartMsmqIfNecessary();

                if (!msmqIsGood)
                {
                    WriteWarning("Msmq may need to be reinstalled manually. Please ensure MSMQ is running properly.");
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
