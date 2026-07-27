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
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointOne" });
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointTwo" });
        diagnostics.Add("Version", new { Version = "1.0.0.0" });

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldWriteEntriesWithTypesUsingTheFullName()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("TypeIndicator", new { SomeType = typeof(DiagnosticsWriterTests) });

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldSupportWritingToLogAndWriter()
    {
        var (writer, output) = CreateCaptureWriter(true);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointOne" });

        await writer.Write(diagnostics.entries);

        Approver.Verify(output() + Environment.NewLine + logStatements, s => TimestampScrubber().Replace(s, "<timestamp>"));
    }

    [Test]
    public async Task ShouldSupportWritingToLogEvenWhenWriterIsNoOp()
    {
        var writer = new HostStartupDiagnosticsWriter(NoOpWriter, true, true);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointOne" });

        await writer.Write(diagnostics.entries);

        Approver.Verify(logStatements.ToString(), s => TimestampScrubber().Replace(s, "<timestamp>"));
    }

    [Test]
    public async Task ShouldSupportWritingToLogEvenWhenWriterFails()
    {
        var writer = new HostStartupDiagnosticsWriter(FailingWriter, true, true);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Endpoint", new { EndpointName = "MyEndpointOne" });

        await writer.Write(diagnostics.entries);

        Approver.Verify(logStatements.ToString(), inputToScrub => StackTraceScrubber.ScrubFileInfoFromStackTrace(TimestampScrubber().Replace(inputToScrub, "<timestamp>")));
    }

    [Test]
    public async Task ShouldWriteAlphabeticalSectionOrder()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Zulu", new { Value = "z" });
        diagnostics.Add("Mike", new { Value = "m" });
        diagnostics.Add("Alpha", new { Value = "a" });

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldWriteEscapedPropertyNames()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Special", new Dictionary<string, object>
        {
            { "Property With Spaces", true },
            { "Normal", 42 }
        });

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldWriteNullAndNestedValues()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("NullValue", new { Value = default(object) });
        diagnostics.Add("Nested", new { Inner = new { Deep = "value" } });

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldInvokeLazySectionOnce()
    {
        var invocationCount = 0;
        var (writer, _) = CreateCaptureWriter(true);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Lazy", new Func<object>(() =>
        {
            invocationCount++;
            return new { Value = "lazy" };
        }));

        await writer.Write(diagnostics.entries);

        Assert.That(invocationCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ShouldCompactAssemblyScanningOnlyInLog()
    {
        var (writer, output) = CreateCaptureWriter(true);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("AssemblyScanning", new AssemblyScanningDiagnostics(
            [new AssemblyDetails("MyAssembly", "1.0.0.0")],
            [],
            false,
            new AssemblyScannerConfiguration()),
            StartupDiagnosticsJsonContext.Default.AssemblyScanningDiagnostics);

        await writer.Write(diagnostics.entries);

        var logOutput = logStatements.ToString();
        Assert.That(logOutput, Does.Contain("\"Assemblies\":[]"), "Log output should have compacted assemblies");
        Assert.That(output(), Does.Contain("MyAssembly"), "Custom writer output should have full assemblies");
    }

    [Test]
    public async Task ShouldTruncateLogAtThreshold()
    {
        var writer = new HostStartupDiagnosticsWriter(NoOpWriter, true, true);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Large", new { Data = new string('X', 40000) });

        await writer.Write(diagnostics.entries);

        var logOutput = logStatements.ToString();
        Assert.That(logOutput, Does.Contain("... (truncated)"));
    }

    [Test]
    public async Task ShouldWriteTypedSection()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Typed", new EndpointDiagnostics
        {
            Name = "MyEndpoint",
            SendOnly = false,
            NServiceBusVersion = "1.0.0"
        }, StartupDiagnosticsJsonContext.Default.EndpointDiagnostics);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldInvokeTypedFactoryOnce()
    {
        var invocationCount = 0;
        var (writer, output) = CreateCaptureWriter(true);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.AddFactory("Factory", () =>
        {
            invocationCount++;
            return new EndpointDiagnostics
            {
                Name = "FromFactory",
                SendOnly = false,
                NServiceBusVersion = "1.0.0"
            };
        }, StartupDiagnosticsJsonContext.Default.EndpointDiagnostics);

        await writer.Write(diagnostics.entries);

        Assert.That(invocationCount, Is.EqualTo(1));
        Assert.That(output(), Does.Contain("FromFactory"));
    }

    [Test]
    public async Task ShouldWriteMixedTypedAndLegacySections()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Legacy", new { Value = "old" });
        diagnostics.Add("Typed", new EndpointDiagnostics
        {
            Name = "MyEndpoint",
            SendOnly = false,
            NServiceBusVersion = "1.0.0"
        }, StartupDiagnosticsJsonContext.Default.EndpointDiagnostics);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldWriteTypedDuplicateEntries()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("Section", new EndpointDiagnostics
        {
            Name = "First",
            SendOnly = false,
            NServiceBusVersion = "1.0.0"
        }, StartupDiagnosticsJsonContext.Default.EndpointDiagnostics);
        diagnostics.Add("Section", new EndpointDiagnostics
        {
            Name = "Second",
            SendOnly = false,
            NServiceBusVersion = "2.0.0"
        }, StartupDiagnosticsJsonContext.Default.EndpointDiagnostics);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldWriteTypedSectionWithSystemType()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("WithType", new ContainerDiagnostics
        {
            Type = typeof(DiagnosticsWriterTests).FullName!
        }, StartupDiagnosticsJsonContext.Default.ContainerDiagnostics);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    [Test]
    public async Task ShouldWriteTypedSectionWithNestedCollections()
    {
        var (writer, output) = CreateCaptureWriter(false);
        var diagnostics = new StartupDiagnosticEntries();
        diagnostics.Add("NestedCollections", new ReceivingDiagnostics
        {
            LocalQueueAddress = new QueueAddressDiagnostics
            {
                BaseAddress = "myqueue",
                Discriminator = null,
                Properties = [],
                Qualifier = null
            },
            InstanceSpecificQueueAddress = null,
            PurgeOnStartup = false,
            TransactionMode = "TransactionScope",
            MaxConcurrency = 10,
            Satellites =
            [
                new SatelliteDiagnostics
                {
                    Name = "Sat1",
                    ReceiveAddress = new QueueAddressDiagnostics
                    {
                        BaseAddress = "satqueue",
                        Discriminator = null,
                        Properties = [],
                        Qualifier = null
                    },
                    MaxConcurrency = 5
                }
            ],
            MessageHandlers = new Dictionary<string, List<string>>
            {
                { "MsgType1", ["Handler1", "Handler2"] }
            }
        }, StartupDiagnosticsJsonContext.Default.ReceivingDiagnostics);

        await writer.Write(diagnostics.entries);

        Approver.Verify(output());
    }

    static (HostStartupDiagnosticsWriter Writer, Func<string> GetOutput) CreateCaptureWriter(bool writeToLog)
    {
        var output = string.Empty;
        var testWriter = new Func<string, CancellationToken, Task>((diagnosticOutput, _) =>
        {
            output = diagnosticOutput;
            return Task.CompletedTask;
        });
        return (new HostStartupDiagnosticsWriter(testWriter, true, writeToLog), () => output);
    }

    static Task NoOpWriter(string _, CancellationToken cancellationToken) => Task.CompletedTask;

    static Task FailingWriter(string _, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException("Test"));

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}", RegexOptions.Compiled)]
    private static partial Regex TimestampScrubber();
}
