namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_endpoint_starts_with_instrumentation_options : NServiceBusAcceptanceTest
{
    static string basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, TestContext.CurrentContext.Test.ID);

    [Test]
    public async Task Should_write_instrumentation_options_to_startup_diagnostics()
    {
        // TestContext.CurrentContext.Test.ID is stable across test runs,
        // therefore we need to clear existing diagnostics file to avoid asserting on a stale file
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }

        await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var endpointName = Conventions.EndpointNamingConvention(typeof(MyEndpoint));
        var pathToFile = Path.Combine(basePath, $"{endpointName}-configuration.txt");

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(pathToFile));
        var section = document.RootElement.GetProperty("OpenTelemetry");

        Assert.Multiple(() =>
        {
            Assert.That(section.GetProperty("SendTraceMode").GetString(), Is.EqualTo("StartNew"));
            Assert.That(section.GetProperty("PublishTraceMode").GetString(), Is.EqualTo("ContinueExisting"));
            Assert.That(section.GetProperty("ExceptionRecordingMode").GetString(), Is.EqualTo("Logs"));
            Assert.That(section.GetProperty("Recoverability").GetProperty("DelayedRetryTraceMode").GetString(), Is.EqualTo("ContinueExisting"));
            Assert.That(section.GetProperty("DelayedDelivery").GetProperty("SendOperationTraceMode").GetString(), Is.EqualTo("ContinueExisting"));
            Assert.That(section.GetProperty("DelayedDelivery").GetProperty("SagaTimeoutTraceMode").GetString(), Is.EqualTo("ContinueExisting"));
            Assert.That(section.GetProperty("UseV11Behavior").ValueKind, Is.EqualTo(JsonValueKind.False).Or.EqualTo(JsonValueKind.True));
        });
    }

    class Context : ScenarioContext;

    public class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.SetDiagnosticsPath(basePath);

                // Flip every option away from its default so the test proves the configured values are written
                var tracing = c.Tracing();
                tracing.SendTraceMode = TraceMode.StartNew;
                tracing.PublishTraceMode = TraceMode.ContinueExisting;
                tracing.ExceptionRecordingMode = ExceptionRecordingMode.Logs;
                tracing.Recoverability.DelayedRetryTraceMode = TraceMode.ContinueExisting;
                tracing.DelayedDelivery.SendOperationTraceMode = TraceMode.ContinueExisting;
                tracing.DelayedDelivery.SagaTimeoutTraceMode = TraceMode.ContinueExisting;
            }).EnableStartupDiagnostics();
    }
}
