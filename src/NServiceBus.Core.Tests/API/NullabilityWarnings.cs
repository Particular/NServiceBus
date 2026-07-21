namespace NServiceBus.Core.Tests.API;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Particular.Approvals;

// As part of the nullable reference type migration effort, individual folders are annotated with
// #nullable enable one at a time (tracked in NullableEnable.CompletedFolders.approved.txt and
// NullableEnable.IncompleteFolders.approved.txt). This test previews what would happen if nullable
// reference types were force-enabled for the whole project: files that already opt in via
// #nullable enable are unaffected, but any file without an explicit directive picks up the
// project-wide default instead of staying oblivious. This test captures the current set of
// resulting warnings so that any new regressions are immediately visible. The goal is to keep the
// list shrinking over time. Once all warnings are resolved
// and the approved file empty, this test can be deleted and <Nullable>enable</Nullable> can be set
// directly in the NServiceBus.Core.csproj.
//
// Only warnings matching the hardcoded set of nullable reference type diagnostic IDs below are captured
// (https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings),
//
// See https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references for more details.
[TestFixture]
public partial class NullabilityWarnings
{
    [Test]
    [CancelAfter(30_000)]
    public async Task ApproveNullabilityWarnings(CancellationToken cancellationToken = default)
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "NServiceBus.Core",
            "NServiceBus.Core.csproj"));

        var binlogPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "out.binlog");

        try
        {
            var warnings = await BuildWithNullableEnabled(projectPath, binlogPath, cancellationToken);

            Approver.Verify(warnings);
        }
        finally
        {
            if (File.Exists(binlogPath))
            {
                File.Delete(binlogPath);
            }
        }
    }

    static async Task<string> BuildWithNullableEnabled(string projectPath, string binlogPath, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-p:Nullable=enable");
        startInfo.ArgumentList.Add("-p:TreatWarningsAsErrors=false");
        startInfo.ArgumentList.Add("-p:IsPackable=false");
        startInfo.ArgumentList.Add($"-bl:{binlogPath}");

        using var process = Process.Start(startInfo)!;

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        Assert.That(process.ExitCode, Is.Zero, $"Build failed:{Environment.NewLine}{error}{Environment.NewLine}{output}");

        var warnings = NullableWarningRegex().Matches(output)
            .Select(m => ScrubLine(m.Value.Trim()))
            .Distinct()
            .OrderBy(w => w, StringComparer.Ordinal)
            .ToList();

        var grouped = warnings
            .GroupBy(w => FileRegex().Match(w).Groups["file"].Value)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        var result = new StringBuilder()
            .AppendLine("The following nullable warnings are present in NServiceBus.Core.")
            .AppendLine("Changes that make this list longer should not be approved.")
            .AppendLine("-----");

        foreach (var group in grouped)
        {
            _ = result.AppendLine().AppendLine(group.Key);
            foreach (var warning in group)
            {
                _ = result.AppendLine($"  {MessageRegex().Match(warning).Groups["msg"].Value}");
            }
        }

        return result.ToString();
    }

    static string ScrubLine(string line)
    {
        line = PathPrefixRegex().Replace(line, "", 1);
        line = line.Replace('\\', '/');
        line = LineNumbersRegex().Replace(line, "");
        line = ProjectPathSuffixRegex().Replace(line, "");
        return line;
    }

    [GeneratedRegex(@"^.+?(?=src[\\/])", RegexOptions.IgnoreCase)]
    private static partial Regex PathPrefixRegex();

    [GeneratedRegex(@"\(\d+,\d+\)")]
    private static partial Regex LineNumbersRegex();

    [GeneratedRegex(@"\s*\[[^\]]+[/\\][^\]]+\]$")]
    private static partial Regex ProjectPathSuffixRegex();

    [GeneratedRegex($@".+: warning CS({NullableWarningCodes}):.+")]
    private static partial Regex NullableWarningRegex();

    [GeneratedRegex(@"^(?<file>src/[^\s:]+)")]
    private static partial Regex FileRegex();

    [GeneratedRegex($@": warning (?<msg>CS({NullableWarningCodes}):.+)$")]
    private static partial Regex MessageRegex();

    // Sourced from https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings
    const string NullableWarningCodes =
        "8597|8598|8600|8601|8602|8603|8604|8605|8607|8608|8609|8610|8611|8612|8613|8614|" +
        "8615|8616|8617|8618|8619|8620|8621|8622|8623|8624|8625|8628|8629|8631|8632|8633|" +
        "8634|8636|8637|8639|8643|8644|8645|8650|8651|8655|8667|8668|8669|8670|8714|8762|" +
        "8763|8764|8765|8766|8767|8768|8769|8770|8774|8775|8776|8777|8819|8824|8825|8847";
}
