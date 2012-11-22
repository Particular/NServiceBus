namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using Setup.Windows.PerformanceCounters;

    [Cmdlet(VerbsLifecycle.Install, "NServiceBusPerformanceCounters")]
    public class InstallPerformanceCounters : CmdletBase
    {
        protected override void Process()
        {
            PerformanceCounterSetup.SetupCounters();
        }
    }

    [Cmdlet(VerbsDiagnostic.Test, "NServiceBusPerformanceCountersInstallation")]
    public class ValidatePerformanceCounters : CmdletBase
    {
        protected override void Process()
        {
            var coutersIsGood = PerformanceCounterSetup.CheckCounters();

            Host.UI.WriteLine(coutersIsGood
                                        ? "NServiceBus Performance Counters are setup and ready for use with NServiceBus."
                                        : "NServiceBus Performance Counters are not properly configured.");

            WriteObject(coutersIsGood);
        }
    }
}
