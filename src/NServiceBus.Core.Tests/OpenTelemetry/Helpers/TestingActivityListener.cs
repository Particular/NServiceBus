namespace NServiceBus.Core.Tests.OpenTelemetry.Helpers;

using System;
using System.Diagnostics;

class TestingActivityListener : IDisposable
{
    readonly ActivityListener activityListener;

    public static TestingActivityListener SetupNServiceBusDiagnosticListener(ActivitySamplingResult samplingResult = ActivitySamplingResult.AllData) =>
        SetupDiagnosticListener(ActivitySources.Main.Name, samplingResult);

    public static TestingActivityListener SetupDiagnosticListener(string sourceName, ActivitySamplingResult samplingResult = ActivitySamplingResult.AllData)
    {
        var testingListener = new TestingActivityListener(sourceName, samplingResult);

        ActivitySource.AddActivityListener(testingListener.activityListener);
        return testingListener;
    }

    TestingActivityListener(string sourceName = null, ActivitySamplingResult samplingResult = ActivitySamplingResult.AllData)
    {
        // do not rely on activities from the notifications as tests are run in parallel
        activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.IsNullOrEmpty(sourceName) || source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => samplingResult,
            SampleUsingParentId = (ref ActivityCreationOptions<string> options) => samplingResult
        };
    }
    public void Dispose() => activityListener?.Dispose();
}