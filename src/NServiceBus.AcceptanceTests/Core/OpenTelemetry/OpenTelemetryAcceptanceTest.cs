namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using NUnit.Framework;
using Traces;

[NonParallelizable] // Ensure only activities for the current test are captured
public abstract class OpenTelemetryAcceptanceTest : NServiceBusAcceptanceTest
{
    protected TestingActivityListener NServicebusActivityListener { get; private set; }

    [SetUp]
    public void Setup() => NServicebusActivityListener = TestingActivityListener.SetupDiagnosticListener("NServiceBus.Core");

    [TearDown]
    public void Cleanup()
    {
        NServicebusActivityListener?.VerifyAllActivitiesCompleted();
        NServicebusActivityListener?.Dispose();
    }
}