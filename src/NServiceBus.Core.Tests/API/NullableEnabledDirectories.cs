namespace NServiceBus.Core.Tests.API;

using System;
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
    [Explicit("Run this test to generate a report of directories that are not fully annotated with '#nullable enable'.")]
    public void GenerateIncompleteDirectoriesReport()
    {
        var sourceRoot = FindSourceRoot();
        var approvalFilesDirectory = Path.Combine(sourceRoot, "NServiceBus.Core.Tests", "ApprovalFiles");
        var incompleteReportFilePath = Path.Combine(approvalFilesDirectory, IncompleteReceivedFileName);

        var incompleteReport = BuildIncompleteDirectoriesReport(sourceRoot);

        if (Path.Exists(incompleteReport))
        {
            File.Delete(incompleteReportFilePath);
        }

        File.WriteAllText(incompleteReportFilePath, incompleteReport);
    }

    static string BuildIncompleteDirectoriesReport(string sourceRoot)
    {
        var builder = new StringBuilder()
            .AppendLine("The following directories have at least one .cs file that is NOT annotated with #nullable enable.")
            .AppendLine("Format: directory<TAB>annotated/total files.")
            .AppendLine("-----")
            .AppendLine("This file is for analysis purposes only and should not be committed.");

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
    const string IncompleteReceivedFileName = "NullableEnable.IncompleteFolders.txt";
}
