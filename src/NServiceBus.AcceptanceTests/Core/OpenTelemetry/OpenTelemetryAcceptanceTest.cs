namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using NUnit.Framework;
using Traces;

[NonParallelizable] // Ensure only activities for the current test are captured
public abstract class OpenTelemetryAcceptanceTest : NServiceBusAcceptanceTest
{
    protected TestingActivityListener NServiceBusActivityListener { get; private set; }

    [SetUp]
    public void Setup() => NServiceBusActivityListener = TestingActivityListener.SetupDiagnosticListener(CoreSource, HandlerSource, RecoverabilitySource);

    protected void DisableTracing()
    {
        NServiceBusActivityListener.Dispose();
        NServiceBusActivityListener = null;
    }

    protected void DisableHandlerTracing()
    {
        NServiceBusActivityListener.Dispose();
        NServiceBusActivityListener = TestingActivityListener.SetupDiagnosticListener(CoreSource, RecoverabilitySource);
    }

    [TearDown]
    public void Cleanup()
    {
        NServiceBusActivityListener?.VerifyAllActivitiesCompleted();
        NServiceBusActivityListener?.Dispose();
    }

    const string CoreSource = "NServiceBus.Core";
    const string HandlerSource = "NServiceBus.Core.Handler";
    const string RecoverabilitySource = "NServiceBus.Core.Recoverability";
}