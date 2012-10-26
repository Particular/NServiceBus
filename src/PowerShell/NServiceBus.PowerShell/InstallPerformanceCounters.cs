namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.PerformanceCounters;

    [Cmdlet(VerbsLifecycle.Install, "PerformanceCounters", SupportsShouldProcess = true)]
    public class InstallPerformanceCounters : CmdletBase
    {
        protected override void Process()
        {
            bool coutersIsGood;
            if (!ShouldProcess(Environment.MachineName))
            {
                coutersIsGood = PerformanceCounterSetup.SetupCounters();

                Host.UI.WriteLine(coutersIsGood
                                          ? "Performance Counters is setup and ready for use with NServiceBus"
                                          : "Performance Counters is not properly configured");

                WriteObject(coutersIsGood);
                return;
            }
            
            coutersIsGood = PerformanceCounterSetup.SetupCounters(true);

            WriteObject(coutersIsGood);
        }
    }
}
