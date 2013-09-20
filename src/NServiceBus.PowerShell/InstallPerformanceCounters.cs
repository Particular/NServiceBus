namespace NServiceBus.PowerShell
{
    using System;
    using System.Management.Automation;
    using Setup.Windows.PerformanceCounters;

    [Cmdlet(VerbsLifecycle.Install, "NServiceBusPerformanceCounters", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    public class InstallPerformanceCounters : CmdletBase
    {

        [Parameter(Mandatory = false, HelpMessage = "Force re-creation of performance counters if they already exist.")]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Environment.MachineName))
            {
                return;
            }
            if (Force)
            {
                ForceCreate();
            }
            else
            {
                Create();   
            }
        }

        void Create()
        {
            var allCountersExist = PerformanceCounterSetup.DoAllCountersExist();
            if (allCountersExist)
            {
                Host.UI.WriteLine("Did not create counters since they already exist");
                return;
            }

            if (PerformanceCounterSetup.DoesCategoryExist())
            {
                var exception = new Exception("Existing category is not configured correctly. Use the -Force option to delete and re-create");
                var errorRecord = new ErrorRecord(exception, "FailedToCreateCategory", ErrorCategory.NotSpecified, null);
                ThrowTerminatingError(errorRecord);
            }

            Host.UI.WriteLine("Creating counters");
            PerformanceCounterSetup.SetupCounters();
        }

        void ForceCreate()
        {
            try
            {
                Host.UI.WriteLine("Deleting counters");
                PerformanceCounterSetup.DeleteCategory();
            }
            catch (Exception exception)
            {
                var errorRecord = new ErrorRecord(exception, "FailedToDeleteCategory", ErrorCategory.NotSpecified, null);
                ThrowTerminatingError(errorRecord);
            }
            Host.UI.WriteLine("Creating counters");
            PerformanceCounterSetup.SetupCounters();
        }
    }
}
