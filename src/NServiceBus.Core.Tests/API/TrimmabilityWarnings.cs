namespace NServiceBus.Core.Tests.API;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Particular.Approvals;

/// <summary>
/// As part of the assembly scanning efforts, many code paths that require dynamically
/// referenced code have been annotated or restructured to satisfy the trimming analyzer.
/// This test captures the current set of trimming warnings so that any new regressions
/// are immediately visible. When the test fails because new warnings appeared, revisit
/// the changes and consider whether the dynamic access can be avoided. It is acceptable
/// to approve new warnings in minor releases when truly necessary, but the goal is to
/// keep the list shrinking over time and never grow without deliberate justification.
/// Once all warnings are resolved and the approved file is empty, this test can be
/// deleted and trimming warnings can be enabled directly in NServiceBus.Core.csproj.
/// <see href="https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming"/>
/// </summary>
[TestFixture]
public partial class TrimmabilityWarnings
{
    [Test]
    public async Task ApproveTrimmabilityWarnings()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "NServiceBus.Core",
            "NServiceBus.Core.csproj"));

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
        startInfo.ArgumentList.Add("-p:EnableTrimAnalyzer=true");
        startInfo.ArgumentList.Add("-p:TreatWarningsAsErrors=false");
        startInfo.ArgumentList.Add("-p:IsPackable=false");
        startInfo.ArgumentList.Add("-bl:out.binlog");
        startInfo.ArgumentList.Add("--no-incremental");

        using var process = Process.Start(startInfo)!;

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        Assert.That(process.ExitCode, Is.Zero, $"Build failed:{Environment.NewLine}{error}{Environment.NewLine}{output}");

        var warnings = ILWarningRegex().Matches(output)
            .Select(m => ScrubLine(m.Value.Trim()))
            .Distinct()
            .OrderBy(w => w, StringComparer.Ordinal)
            .ToList();

        var grouped = warnings
            .GroupBy(w => FileRegex().Match(w).Groups["file"].Value)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        var result = new StringBuilder()
            .AppendLine("The following trimming warnings are present in NServiceBus.Core.")
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

        Approver.Verify(result.ToString());
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

    [GeneratedRegex(@"\s*\[.+\]$")]
    private static partial Regex ProjectPathSuffixRegex();

    [GeneratedRegex(@".+: warning IL[23]\d{3}.+")]
    private static partial Regex ILWarningRegex();

    [GeneratedRegex(@"^(?<file>src/[^\s:]+)")]
    private static partial Regex FileRegex();

    [GeneratedRegex(@": warning (?<msg>IL[23]\d{3}:.+)$")]
    private static partial Regex MessageRegex();
}