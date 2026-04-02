namespace NServiceBus.TransportTests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

sealed class TestingActivityListener : IDisposable
{
    readonly ActivityListener activityListener;
    readonly HashSet<string> sourceNames;

    public TestingActivityListener(params string[] sourceNames)
    {
        this.sourceNames = [.. sourceNames];
        activityListener = new ActivityListener
        {
            ShouldListenTo = source => this.sourceNames.Count == 0 || this.sourceNames.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        activityListener.ActivityStarted += activity => StartedActivities.Enqueue(activity);
        activityListener.ActivityStopped += activity => CompletedActivities.Enqueue(activity);

        ActivitySource.AddActivityListener(activityListener);
    }

    public ConcurrentQueue<Activity> StartedActivities { get; } = new();

    public ConcurrentQueue<Activity> CompletedActivities { get; } = new();

    public IReadOnlyList<Activity> CompletedFrom(string sourceName) =>
        CompletedActivities.Where(activity => activity.Source.Name == sourceName).ToList();

    public void Dispose() => activityListener.Dispose();
}
