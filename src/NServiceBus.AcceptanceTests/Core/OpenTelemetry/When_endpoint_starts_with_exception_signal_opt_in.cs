namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_endpoint_starts_with_exception_signal_opt_in : OpenTelemetryAcceptanceTest
{
    [TestCase(null, "SpanAndLogs")]
    [TestCase("LOGS", "Logs")]
    [TestCase("logs/dup", "SpanAndLogs")]
    public async Task Should_write_the_effective_exception_recording_mode_to_startup_diagnostics(string optIn, string expected) =>
        Assert.That(await GetExceptionRecordingModeFromDiagnostics(optIn), Is.EqualTo(expected));

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_write_logs_with_v11_behavior_whatever_the_opt_in_says() =>
        Assert.That(await GetExceptionRecordingModeFromDiagnostics(ExceptionSignalOptIn.SpanAndLogs), Is.EqualTo("Logs"));

    static async Task<string> GetExceptionRecordingModeFromDiagnostics(string optIn)
    {
        var basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, TestContext.CurrentContext.Test.ID);
        DeleteDiagnosticsOfPreviousTestRun(basePath);

        await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>(e => e.CustomConfig(c =>
            {
                c.SetDiagnosticsPath(basePath);
                if (optIn != null)
                {
                    c.SetExceptionSignalOptIn(optIn);
                }
            }))
            .Done(c => c.EndpointsStarted)
            .Run();

        var endpointName = Conventions.EndpointNamingConvention(typeof(MyEndpoint));
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(basePath, $"{endpointName}-configuration.txt")));

        return document.RootElement.GetProperty("OpenTelemetry").GetProperty("ExceptionRecordingMode").GetString();
    }

    static void DeleteDiagnosticsOfPreviousTestRun(string basePath)
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }
    }

    class Context : ScenarioContext;

    public class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint() => EndpointSetup<DefaultServer>().EnableStartupDiagnostics();
    }
}
