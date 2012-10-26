namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.Dtc;

    [Cmdlet(VerbsLifecycle.Install, "Dtc", SupportsShouldProcess = true)]
    public class InstallDtc : CmdletBase
    {
        protected override void Process()
        {
            bool dtcIsGood;
            if (!ShouldProcess(Environment.MachineName))
            {
                dtcIsGood = DtcSetup.StartDtcIfNecessary();
                Host.UI.WriteLine(dtcIsGood
                                          ? "DTC is setup and ready for use with NServiceBus"
                                          : "DTC is not properly configured");

                WriteObject(dtcIsGood);
                return;
            }
            
            dtcIsGood = DtcSetup.StartDtcIfNecessary(true);

            WriteObject(dtcIsGood);
        }
    }
}
