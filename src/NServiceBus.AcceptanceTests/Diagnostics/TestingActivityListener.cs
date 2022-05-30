namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using NUnit.Framework;

    class TestingActivityListener : IDisposable
    {
        readonly ActivityListener activityListener;

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
                TestContext.WriteLine($"Activity {activity.Id} started by {GetHashCode()}");
                StartedActivities.Enqueue(activity);
            };
            activityListener.ActivityStopped += activity =>
            {
                TestContext.WriteLine($"Activity {activity.Id} completed by {GetHashCode()}");
                CompletedActivities.Enqueue(activity);
            };
        }

        public void Dispose() => activityListener?.Dispose();

        public ConcurrentQueue<Activity> StartedActivities { get; } = new ConcurrentQueue<Activity>();
        public ConcurrentQueue<Activity> CompletedActivities { get; } = new ConcurrentQueue<Activity>();
    }

    static class ActivityExtensions
    {
        public static List<Activity> GetIncomingActivities(this ConcurrentQueue<Activity> activities) => activities.Where(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage").ToList();
        public static List<Activity> GetOutgoingActivities(this ConcurrentQueue<Activity> activities) => activities.Where(a => a.OperationName == "NServiceBus.Diagnostics.OutgoingMessage").ToList();
    }
}
