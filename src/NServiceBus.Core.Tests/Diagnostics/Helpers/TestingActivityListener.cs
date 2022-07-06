namespace NServiceBus.Core.Tests.Diagnostics.Helpers;

using System;
using System.Diagnostics;

class TestingActivityListener : IDisposable
{
    readonly ActivityListener activityListener;

    public static TestingActivityListener SetupNServiceBusDiagnosticListener() => SetupDiagnosticListener(ActivitySources.Main.Name);

    public static TestingActivityListener SetupDiagnosticListener(string sourceName)
    {
        var testingListener = new TestingActivityListener(sourceName);

        ActivitySource.AddActivityListener(testingListener.activityListener);
        return testingListener;
    }

    TestingActivityListener(string sourceName = null)
    {
        // do not rely on activities from the notifications as tests are run in parallel
        activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.IsNullOrEmpty(sourceName) || source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData
        };
    }
    public void Dispose() => activityListener?.Dispose();
}