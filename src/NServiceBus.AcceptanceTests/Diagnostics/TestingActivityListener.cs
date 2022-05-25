namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Generic;
    using System.Diagnostics;
    class TestingActivityListener
    {
        ActivityListener activityListener;

        public static TestingActivityListener SetupNServiceBusDiagnosticListener() => SetupDiagnosticListener("NServiceBus.Diagnostics");

        public static TestingActivityListener SetupDiagnosticListener(string sourceName)
        {
            var testingListener = new TestingActivityListener(sourceName);

            ActivitySource.AddActivityListener(testingListener.activityListener);
            return testingListener;
        }

        TestingActivityListener(string sourceName = null)
        {
            activityListener = new ActivityListener
            {
                ShouldListenTo = source => string.IsNullOrEmpty(sourceName) || source.Name == sourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData
            };
            activityListener.ActivityStarted += activity =>
            {
                StartedActivities.Add(activity);
            };
            activityListener.ActivityStopped += activity =>
            {
                CompletedActivities.Add(activity);
            };
        }

        public List<Activity> StartedActivities { get; } = new List<Activity>();
        public List<Activity> CompletedActivities { get; } = new List<Activity>();
    }
}
