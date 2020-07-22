namespace NServiceBus.Core.Tests.Diagnostics
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class DiagnosticsWriterTests
    {
        [Test]
        public async Task ShouldWriteWhenDuplicateEntriesPresent()
        {
            var output = string.Empty;
            var testWriter = new Func<string, Task>(diagnosticOutput =>
            {
                output = diagnosticOutput;
                return TaskEx.CompletedTask;
            });
            var diagnostics = new StartupDiagnosticEntries();
            diagnostics.Add("Endpoint", new { Name = "MyEndpointOne" });
            diagnostics.Add("Endpoint", new { Name = "MyEndpointTwo" });
            
            var writer = new HostStartupDiagnosticsWriter(testWriter, true);

            await writer.Write(diagnostics.entries);

            Assert.IsNotEmpty(output);
            Assert.True(output.Contains("MyEndpointOne"));
            Assert.False(output.Contains("MyEndpointTwo"));
        }
    }
}