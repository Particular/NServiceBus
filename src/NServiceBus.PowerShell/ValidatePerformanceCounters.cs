namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using Setup.Windows.PerformanceCounters;

    [Cmdlet(VerbsDiagnostic.Test, "NServiceBusPerformanceCountersInstallation")]
    public class ValidatePerformanceCounters : CmdletBase
    {
        protected override void ProcessRecord()
        {
            var countersAreGood = PerformanceCounterSetup.CheckCounters();

            WriteVerbose(countersAreGood
                ? "NServiceBus Performance Counters are setup and ready for use with NServiceBus."
                : "NServiceBus Performance Counters are not properly configured.");

            WriteObject(countersAreGood);
        }
    }
}