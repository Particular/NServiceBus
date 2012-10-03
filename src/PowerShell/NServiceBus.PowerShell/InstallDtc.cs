namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.Dtc;

    [Cmdlet(VerbsLifecycle.Install, "Dtc")]
    public class InstallDtc : PSCmdlet
    {

        [Parameter(HelpMessage = "Checks if DTC is setup properly without fixing any potential issues")]
        public SwitchParameter WhatIf { get; set; }

        protected override void ProcessRecord()
        {
            bool dtcIsGood;
            if (WhatIf)
            {
                dtcIsGood = DtcSetup.StartDtcIfNecessary(false);
                Console.Out.WriteLine(dtcIsGood
                                          ? "DTC is setup and ready for use with NServiceBus"
                                          : "DTC is not properly configured, if you rerun the command without -WhatIf DTC will be setup automatically for you");

                WriteObject(dtcIsGood);
                return;
            }

            dtcIsGood = DtcSetup.StartDtcIfNecessary(true);

            WriteObject(dtcIsGood);
        }
    }
}
