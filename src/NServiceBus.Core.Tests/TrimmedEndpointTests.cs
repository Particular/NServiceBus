namespace NServiceBus.Core.Tests.TrimmedEndpoint;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class TrimmedEndpointTests
{
    [Test]
    [CancelAfter(600_000)]
    public async Task Scanner_disabled_endpoint_publishes_trimmed_and_processes_a_message(CancellationToken cancellationToken = default)
    {
        var sampleProject = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "NServiceBus.TrimmedEndpoint",
            "NServiceBus.TrimmedEndpoint.csproj"));

        var publishOutput = Path.Combine(Path.GetTempPath(), "nservicebus-trimmed-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var publishResult = await RunProcess("dotnet",
                $"publish \"{sampleProject}\" -c Release -p:TreatWarningsAsErrors=false -o \"{publishOutput}\" --nologo",
                cancellationToken);

            Assert.That(publishResult.ExitCode, Is.Zero, $"Publish failed:{Environment.NewLine}{publishResult.Output}");

            // The AddMessageType and AddHandler calls in the sample are intercepted by source generators. If they
            // were not intercepted, the RequiresUnreferencedCode fallback would surface as IL2026 trim warnings at
            // the sample's own call sites. Trim warnings inside NServiceBus.Core itself are tracked separately by
            // the TrimmabilityWarnings approval test.
            var sampleTrimWarnings = publishResult.Output.Split(Environment.NewLine)
                .Where(line => line.Contains("Program.cs") && line.Contains("IL2026"))
                .ToArray();
            Assert.That(sampleTrimWarnings, Is.Empty, "Interception of AddMessageType/AddHandler failed, IL2026 warnings were emitted for the sample source.");

            var executable = Path.Combine(publishOutput, OperatingSystem.IsWindows() ? "TrimmedEndpoint.exe" : "TrimmedEndpoint");
            var runResult = await RunProcess(executable, "", cancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runResult.ExitCode, Is.Zero, $"Trimmed endpoint failed:{Environment.NewLine}{runResult.Output}");
                Assert.That(runResult.Output, Does.Contain("TRIM-VALIDATION-SUCCESS"));
            }
        }
        finally
        {
            try
            {
                Directory.Delete(publishOutput, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    static async Task<ProcessResult> RunProcess(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)!;

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(process.ExitCode, (await outputTask) + Environment.NewLine + (await errorTask));
    }

    sealed record ProcessResult(int ExitCode, string Output);
}
