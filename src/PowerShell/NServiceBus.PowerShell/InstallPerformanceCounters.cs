namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.PerformanceCounters;

    [Cmdlet(VerbsLifecycle.Install, "PerformanceCounters")]
    public class InstallPerformanceCounters : PSCmdlet
    {

        [Parameter(HelpMessage = "Checks if the NServiceBus performance counters is setup properly without fixing any potential issues")]
        public SwitchParameter WhatIf { get; set; }

        protected override void ProcessRecord()
        {
            bool coutersIsGood;
            if (WhatIf)
            {
                coutersIsGood = PerformanceCounterSetup.SetupCounters(false);

                Console.Out.WriteLine(coutersIsGood
                                          ? "Performance Counters is setup and ready for use with NServiceBus"
                                          : "Performance Counters is not properly configured, if you rerun the command without -WhatIf they will be setup automatically for you");

                WriteObject(coutersIsGood);
                return;
            }

            coutersIsGood = PerformanceCounterSetup.SetupCounters(true);

            WriteObject(coutersIsGood);
        }
    }
}
