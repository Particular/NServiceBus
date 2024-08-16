namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

public class TestingActivityListener : IDisposable
{
    readonly ActivityListener activityListener;

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
            StartedActivities.Enqueue(activity);
        };
        activityListener.ActivityStopped += activity =>
        {
            CompletedActivities.Enqueue(activity);
        };
    }

    public void Dispose()
    {
        activityListener?.Dispose();
        GC.SuppressFinalize(this);
    }

    public ConcurrentQueue<Activity> StartedActivities { get; } = new ConcurrentQueue<Activity>();
    public ConcurrentQueue<Activity> CompletedActivities { get; } = new ConcurrentQueue<Activity>();

    public void VerifyAllActivitiesCompleted()
    {
        Assert.That(CompletedActivities.Count, Is.EqualTo(StartedActivities.Count), "all started activities should be completed");
    }
}

static class ActivityExtensions
{
    public static List<Activity> GetReceiveMessageActivities(this ConcurrentQueue<Activity> activities, bool includeControlMessages = false)
        => activities.Where(a => a.OperationName == "NServiceBus.Diagnostics.ReceiveMessage")
                     .Where(a => includeControlMessages || !Convert.ToBoolean(a.GetTagItem("nservicebus.control_message")))
                     .ToList();

    public static List<Activity> GetSendMessageActivities(this ConcurrentQueue<Activity> activities) => activities.Where(a => a.OperationName == "NServiceBus.Diagnostics.SendMessage").ToList();
    public static List<Activity> GetPublishEventActivities(this ConcurrentQueue<Activity> activities) => activities.Where(a => a.OperationName == "NServiceBus.Diagnostics.PublishMessage").ToList();
    public static List<Activity> GetInvokedHandlerActivities(this ConcurrentQueue<Activity> activities) => activities.Where(a => a.OperationName == "NServiceBus.Diagnostics.InvokeHandler").ToList();
}
