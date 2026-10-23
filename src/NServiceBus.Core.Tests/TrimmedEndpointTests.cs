#nullable enable

namespace NServiceBus.Core.Tests.TrimmedEndpoint;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public partial class TrimmedEndpointTests
{
    [Test]
    [CancelAfter(600_000)]
    public async Task Scanner_disabled_endpoint_publishes_trimmed_and_processes_a_message(CancellationToken cancellationToken = default)
    {
        var sampleProject = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "TrimmedEndpoint",
            "TrimmedEndpoint.csproj"));

        var publishOutput = Path.Combine(Path.GetTempPath(), "nservicebus-trimmed-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var publishResult = await RunProcess("dotnet",
                $"publish \"{sampleProject}\" -c Release -p:TreatWarningsAsErrors=false -o \"{publishOutput}\" --nologo",
                cancellationToken);

            Assert.That(publishResult.ExitCode, Is.Zero, $"Publish failed:{Environment.NewLine}{publishResult.Output}");

            // The AddMessageType and AddHandler calls in the sample are intercepted by source generators. If they
            // were not intercepted, the RequiresUnreferencedCode fallback would surface as IL2026 trim warnings at
            // the sample's own call sites. Trim warnings inside NServiceBus.Core itself fail the Core build via
            // EnableTrimAnalyzer.
            var sampleTrimWarnings = FindSampleWarnings(publishResult.Output, sampleProject, "IL2026");
            Assert.That(sampleTrimWarnings, Is.Empty, "IL2026 warnings were emitted for the sample source, including generated code. Interception of AddMessageType/AddHandler may have failed, or generated registrations keep members annotated with RequiresUnreferencedCode.");

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

    [Test]
    // NativeAOT publish on a cold cache can take substantially longer than a trimmed publish.
    [CancelAfter(1_200_000)]
    public async Task Scanner_disabled_endpoint_publishes_and_runs_with_native_aot(CancellationToken cancellationToken = default)
    {
        var runtimeIdentifier = RuntimeIdentifierForCurrentPlatform();
        if (runtimeIdentifier is null)
        {
            Assert.Ignore("NativeAOT is not supported on this platform/architecture.");
            return;
        }

        var sampleProject = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "TrimmedEndpoint",
            "TrimmedEndpoint.csproj"));

        var publishOutput = Path.Combine(Path.GetTempPath(), "nservicebus-aot-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var publishResult = await RunProcess("dotnet",
                $"publish \"{sampleProject}\" -c Release -p:RunAotValidation=true -p:TreatWarningsAsErrors=false -r {runtimeIdentifier} -o \"{publishOutput}\" --nologo",
                cancellationToken);

            Assert.That(publishResult.ExitCode, Is.Zero, $"NativeAOT publish failed:{Environment.NewLine}{publishResult.Output}");

            // The sample source must not rely on dynamic code: any IL2026/IL3050 at the sample's own call sites
            // or in the code generated into it would indicate an intercepted registration fell back to reflection
            // or keeps members annotated with RequiresUnreferencedCode.
            var sampleAotWarnings = FindSampleWarnings(publishResult.Output, sampleProject, "IL2026", "IL3050");
            Assert.That(sampleAotWarnings, Is.Empty, "IL2026/IL3050 warnings were emitted for the sample source, including generated code. Interception of AddMessageType/AddHandler/AddSaga may have failed, or generated registrations keep members annotated with RequiresUnreferencedCode.");

            var executable = Path.Combine(publishOutput, OperatingSystem.IsWindows() ? "TrimmedEndpoint.exe" : "TrimmedEndpoint");
            var runResult = await RunProcess(executable, "", cancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(runResult.ExitCode, Is.Zero, $"NativeAOT endpoint failed:{Environment.NewLine}{runResult.Output}");
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

    // Warnings carry the path of the file they are attributed to, for example Program.cs or a generated file under
    // the project's obj folder. Warnings attributed to files outside the sample project, such as NServiceBus.Core
    // sources, are ignored.
    static string[] FindSampleWarnings(string publishOutput, string sampleProject, params string[] warningCodes)
    {
        var sampleDirectory = Path.GetDirectoryName(sampleProject)! + Path.DirectorySeparatorChar;

        return [.. publishOutput.Split(Environment.NewLine)
            .Where(line => warningCodes.Any(line.Contains))
            .Where(line =>
            {
                var match = WarningFilePath().Match(line);
                return match.Success && match.Groups["path"].Value.StartsWith(sampleDirectory, StringComparison.OrdinalIgnoreCase);
            })
            .Distinct()];
    }

    [GeneratedRegex(@"^\s*(?<path>.+?)\(\d+(,\d+)*\): ")]
    private static partial Regex WarningFilePath();

    static string? RuntimeIdentifierForCurrentPlatform()
    {
        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => null,
            Architecture.Arm => null,
            Architecture.Arm64 => "arm64",
            Architecture.Wasm => null,
            Architecture.S390x => null,
            Architecture.LoongArch64 => null,
            Architecture.Armv6 => null,
            Architecture.Ppc64le => null,
            Architecture.RiscV64 => null,
            _ => null
        };

        if (architecture is null)
        {
            return null;
        }

        if (OperatingSystem.IsWindows())
        {
            return $"win-{architecture}";
        }

        if (OperatingSystem.IsLinux())
        {
            return $"linux-{architecture}";
        }

        if (OperatingSystem.IsMacOS())
        {
            return $"osx-{architecture}";
        }

        return null;
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
