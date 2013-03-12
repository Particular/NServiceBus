namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.Dtc;

    [Cmdlet(VerbsLifecycle.Install, "NServiceBusDTC", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    public class InstallDtc : CmdletBase
    {
        protected override void ProcessRecord()
        {
            if (ShouldProcess(Environment.MachineName))
            {
                DtcSetup.StartDtcIfNecessary();
            }
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "NServiceBusDTCInstallation")]
    public class ValidateDtc : CmdletBase
    {
        protected override void ProcessRecord()
        {
            var dtcIsGood = DtcSetup.IsDtcWorking();
            WriteVerbose(dtcIsGood
                             ? "DTC is setup and ready for use with NServiceBus."
                             : "DTC is not properly configured.");

            WriteObject(dtcIsGood);
        }
    }
}
