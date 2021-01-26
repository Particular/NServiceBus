namespace NServiceBus.Core.Tests.Diagnostics
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Particular.Approvals;

    [TestFixture]
    public class DiagnosticsWriterTests
    {
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

            var writer = new HostStartupDiagnosticsWriter(testWriter, true);

            await writer.Write(diagnostics.entries, default);

            Approver.Verify(output);
        }
    }
}