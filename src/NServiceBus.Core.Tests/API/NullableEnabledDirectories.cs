namespace NServiceBus.Core.Tests.API;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

[TestFixture]
public class NullableEnabledDirectories
{
    [Test]
    public void EnsureFilesInCompletedDirectoriesAreAnnotated()
    {
        var sourceRoot = FindSourceRoot();
        var completedDirectories = ReadCompletedDirectories(sourceRoot);

        var violations = new StringBuilder();

        foreach (var relativeDirectory in completedDirectories)
        {
            var directory = Path.Combine(sourceRoot, relativeDirectory);

            if (!Directory.Exists(directory))
            {
                violations.AppendLine($"{relativeDirectory} (directory listed in {ApprovedFileName} no longer exists)");
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly))
            {
                if (!IsAnnotated(file))
                {
                    violations.AppendLine(Path.GetRelativePath(sourceRoot, file).Replace('\\', '/'));
                }
            }
        }

        if (violations.Length > 0)
        {
            Assert.Fail(
                $"The following files are missing the '#nullable enable' annotation, followed by a blank line, " +
                $"even though they live in a directory listed as fully migrated in {ApprovedFileName}. Either fix " +
                $"the file or, if the directory is no longer fully migrated, remove it from {ApprovedFileName}:{Environment.NewLine}{violations}");
        }
    }

    [Test]
    public void EnsureIncompleteDirectoriesSnapshotIsUpToDate()
    {
        var sourceRoot = FindSourceRoot();
        var approvalFilesDirectory = Path.Combine(sourceRoot, "NServiceBus.Core.Tests", "ApprovalFiles");
        var approvedFilePath = Path.Combine(approvalFilesDirectory, IncompleteApprovedFileName);
        var receivedFilePath = Path.Combine(approvalFilesDirectory, IncompleteReceivedFileName);

        var received = BuildIncompleteDirectoriesReport(sourceRoot);
        var approved = File.Exists(approvedFilePath) ? File.ReadAllText(approvedFilePath) : "";

        if (Normalize(received) == Normalize(approved))
        {
            File.Delete(receivedFilePath);
            return;
        }

        File.WriteAllText(receivedFilePath, received);

        var differences = DescribeDifferences(approved, received);

        Assert.Fail(
            $"{IncompleteApprovedFileName} is out of date: the current annotated/total ratios no longer match. " +
            $"This can happen when a file's annotation state changes, or when a directory reaches 100% and " +
            $"should instead move to {ApprovedFileName}. Compare {receivedFilePath} against {approvedFilePath} " +
            $"and update the approved file to match. Differing directories:{Environment.NewLine}{differences}");
    }

    static string DescribeDifferences(string approvedText, string receivedText)
    {
        var approvedEntries = ParseDirectoryRatios(approvedText);
        var receivedEntries = ParseDirectoryRatios(receivedText);

        var allDirectories = approvedEntries.Keys
            .Union(receivedEntries.Keys, StringComparer.Ordinal)
            .OrderBy(directory => directory, StringComparer.Ordinal);

        var builder = new StringBuilder();

        foreach (var directory in allDirectories)
        {
            var hasApproved = approvedEntries.TryGetValue(directory, out var approvedRatio);
            var hasReceived = receivedEntries.TryGetValue(directory, out var receivedRatio);

            if (!hasApproved)
            {
                builder.AppendLine($"  + {directory}\t{receivedRatio} (newly incomplete - not in {IncompleteApprovedFileName})");
            }
            else if (!hasReceived)
            {
                builder.AppendLine($"  - {directory}\t{approvedRatio} (no longer has any missing annotations - remove from {IncompleteApprovedFileName}, and add to {ApprovedFileName} if fully migrated)");
            }
            else if (approvedRatio != receivedRatio)
            {
                builder.AppendLine($"  ~ {directory}\t{approvedRatio} -> {receivedRatio}");
            }
        }

        return builder.ToString();
    }

    static Dictionary<string, string> ParseDirectoryRatios(string text) =>
        text
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .SkipWhile(line => line != "-----")
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split('\t'))
            .ToDictionary(parts => parts[0], parts => parts[1]);

    static string BuildIncompleteDirectoriesReport(string sourceRoot)
    {
        var builder = new StringBuilder()
            .AppendLine("The following directories have at least one .cs file that is NOT annotated with #nullable enable.")
            .AppendLine("Format: directory<TAB>annotated/total files.")
            .AppendLine("-----");

        var directories = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBinOrObj(file))
            .GroupBy(file => Path.GetDirectoryName(file)!)
            .Select(group => new
            {
                Directory = Path.GetRelativePath(sourceRoot, group.Key).Replace('\\', '/'),
                Total = group.Count(),
                Annotated = group.Count(IsAnnotated)
            })
            .Where(entry => entry.Annotated < entry.Total)
            .OrderBy(entry => entry.Directory, StringComparer.Ordinal);

        foreach (var entry in directories)
        {
            builder.AppendLine($"{entry.Directory}\t{entry.Annotated}/{entry.Total}");
        }

        return builder.ToString();
    }

    static bool IsAnnotated(string file)
    {
        var firstTwoLines = File.ReadLines(file).Take(2).ToArray();
        var firstLine = firstTwoLines.ElementAtOrDefault(0);
        var secondLine = firstTwoLines.ElementAtOrDefault(1);

        return firstLine == "#nullable enable" && secondLine == "";
    }

    static bool IsBinOrObj(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/") || normalized.Contains("/obj/");
    }

    static string Normalize(string text) => text.Replace("\r\n", "\n");

    static string[] ReadCompletedDirectories(string sourceRoot)
    {
        var approvedFilePath = Path.Combine(sourceRoot, "NServiceBus.Core.Tests", "ApprovalFiles", ApprovedFileName);

        return File.ReadAllLines(approvedFilePath)
            .SkipWhile(line => line != "-----")
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    static string FindSourceRoot()
    {
        var directory = TestContext.CurrentContext.TestDirectory;

        while (directory != null)
        {
            if (Directory.GetFiles(directory, "*.csproj").Length == 1)
            {
                return Directory.GetParent(directory)!.FullName;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not find the src directory.");
    }

    const string ApprovedFileName = "NullableEnable.CompletedFolders.approved.txt";
    const string IncompleteApprovedFileName = "NullableEnable.IncompleteFolders.approved.txt";
    const string IncompleteReceivedFileName = "NullableEnable.IncompleteFolders.received.txt";
}
