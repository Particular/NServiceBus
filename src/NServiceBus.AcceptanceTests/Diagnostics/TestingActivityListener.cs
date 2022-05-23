namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System.Collections.Generic;
    using System.Diagnostics;
    class TestingActivityListener
    {
        private ActivityListener activityListener;

        public static TestingActivityListener Setup()
        {
            var testingListener = new TestingActivityListener("NServiceBus.Diagnostics");

            ActivitySource.AddActivityListener(testingListener.activityListener);
            return testingListener;
        }

        private TestingActivityListener(string sourceName = null)
        {
            activityListener = new ActivityListener
            {
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ShouldListenTo = source => string.IsNullOrEmpty(sourceName) || source.Name == sourceName
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
