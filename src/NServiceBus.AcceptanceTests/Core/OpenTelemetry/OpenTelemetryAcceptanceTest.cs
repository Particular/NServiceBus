namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using NUnit.Framework;
using Traces;

[NonParallelizable] // Ensure only activities for the current test are captured
public abstract class OpenTelemetryAcceptanceTest : NServiceBusAcceptanceTest
{
    protected TestingActivityListener NServiceBusActivityListener { get; private set; }

    [SetUp]
    public void Setup() => NServiceBusActivityListener = TestingActivityListener.SetupDiagnosticListener("NServiceBus.Core");

    [TearDown]
    public void Cleanup()
    {
        NServiceBusActivityListener?.VerifyAllActivitiesCompleted();
        NServiceBusActivityListener?.Dispose();
    }
}