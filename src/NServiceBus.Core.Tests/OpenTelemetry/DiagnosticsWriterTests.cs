namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
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

    [Test]
    public async Task ShouldWriteAlphabeticalSectionOrder()
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Zulu", new { Value = "z" });
        diagnostics.Add("Mike", new { Value = "m" });
        diagnostics.Add("Alpha", new { Value = "a" });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, false);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output);
    }

    [Test]
    public async Task ShouldWriteEscapedPropertyNames()
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Special", new Dictionary<string, object>
        {
            { "Property With Spaces", true },
            { "Normal", 42 }
        });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, false);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output);
    }

    [Test]
    public async Task ShouldWriteNullAndNestedValues()
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("NullValue", new { Value = default(object) });
        diagnostics.Add("Nested", new { Inner = new { Deep = "value" } });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, false);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output);
    }

    [Test]
    public async Task ShouldInvokeLazySectionOnce()
    {
        var invocationCount = 0;
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Lazy", new Func<object>(() =>
        {
            invocationCount++;
            return new { Value = "lazy" };
        }));

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, true);

        await writer.Write(diagnostics.entries);

        Assert.That(invocationCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShouldCompactAssemblyScanningOnlyInLog()
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("AssemblyScanning", new AssemblyScanningDiagnostics(
            [new AssemblyDetails("MyAssembly", "1.0.0.0")],
            [],
            false,
            new AssemblyScannerConfiguration()));

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, true);

        await writer.Write(diagnostics.entries);

        // Log output should have compacted (empty assemblies)
        var logOutput = logStatements.ToString();
        Assert.That(logOutput, Does.Contain("\"Assemblies\":[]"), "Log output should have compacted assemblies");

        // Custom writer output should have full assemblies
        Assert.That(output, Does.Contain("MyAssembly"), "Custom writer output should have full assemblies");
    }

    [Test]
    public async Task ShouldTruncateLogAtThreshold()
    {
        var testWriter = new Func<string, CancellationToken, Task>((_, _) => Task.CompletedTask);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Large", new { Data = new string('X', 40000) });

        var writer = new HostStartupDiagnosticsWriter(testWriter, true, true);

        await writer.Write(diagnostics.entries);

        var logOutput = logStatements.ToString();
        Assert.That(logOutput, Does.Contain("... (truncated)"));
    }

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}", RegexOptions.Compiled)]
    private static partial Regex TimestampScrubber();
}