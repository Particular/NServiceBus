namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NUnit.Framework;
using Particular.Approvals;
using Testing;
using Tests.Helpers;

[TestFixture]
public partial class DiagnosticsWriterTests
{
    static StringBuilder logStatements = new StringBuilder();

    [OneTimeSetUp]
    public void LoggerSetup()
    {
#pragma warning disable CS0618 // Use<T> and TestingLoggerFactory (via LoggingFactoryDefinition) are deprecated; test setup uses them intentionally
        LogManager.Use<TestingLoggerFactory>()
            .WriteTo(new StringWriter(logStatements));
#pragma warning restore CS0618
    }

    [TearDown]
    public void TearDown() => logStatements.Clear();

    [Test]
    public async Task ShouldWriteWhenDuplicateEntriesPresent()
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointOne" });
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointTwo" });
        diagnostics.Add("Version", new { Version = "1.0.0.0" });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, false);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output);
    }

    [Test]
    public async Task ShouldWriteEntriesWithTypesUsingTheFullName()
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("TypeIndicator", new { SomeType = typeof(DiagnosticsWriterTests) });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, false);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output);
    }

    [Test]
    public async Task ShouldSupportWritingToLogAndWriter()
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointOne" });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, true);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output + Environment.NewLine + logStatements, s => TimestampScrubber().Replace(s, "<timestamp>"));
    }

    [Test]
    public async Task ShouldSupportWritingToLogEvenWhenWriterIsNoOp()
    {
        var testWriter = new Func<string, CancellationToken, Task>((_, _) => Task.CompletedTask);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointOne" });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, true);

        await writer.Write(diagnostics.entries);

        Approver.Verify(logStatements.ToString(), s => TimestampScrubber().Replace(s, "<timestamp>"));
    }

    [Test]
    public async Task ShouldSupportWritingToLogEvenWhenWriterFails()
    {
        var testWriter = new Func<string, CancellationToken, Task>((_, _) => Task.FromException<InvalidOperationException>(new InvalidOperationException("Test")));
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointOne" });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, true);

        await writer.Write(diagnostics.entries);

        Approver.Verify(logStatements.ToString(), inputToScrub => StackTraceScrubber.ScrubFileInfoFromStackTrace(TimestampScrubber().Replace(inputToScrub, "<timestamp>")));
    }

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}", RegexOptions.Compiled)]
    private static partial Regex TimestampScrubber();
}