// This test project is shipped as a source package (NServiceBus.TransportTests.Sources) to
// downstream transport implementers. By default, Particular.Packaging includes ALL .cs files
// in the package unless explicitly excluded via RemoveSourceFileFromPackage.
//
// This approval test ensures that adding a new .cs file is a deliberate choice:
// - If the file should be shipped to downstream, the approved list must be updated.
// - If the file should NOT be shipped (e.g. it tests learning-transport internals),
//   it must be added to RemoveSourceFileFromPackage in the .csproj.
//
// IMPORTANT: This test itself is excluded from shipping via RemoveSourceFileFromPackage.
// However, when shipping a new source package release, be aware that changes to the approved
// list will also propagate to downstream repositories. Any newly approved file that is not
// excluded may cause downstream test failures until those repositories are updated.
// Consider validating against a select set of downstream repositories before shipping.

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class ShippedSourceFilesApproval
{
    [Test]
    public void ApproveShippedSourceFiles()
    {
        var projectDirectory = TestContext.CurrentContext.TestDirectory;
        while (!File.Exists(Path.Combine(projectDirectory, "NServiceBus.TransportTests.csproj")))
        {
            projectDirectory = Directory.GetParent(projectDirectory)?.FullName
                ?? throw new InvalidOperationException("Could not find project directory.");
        }

        var csprojPath = Path.Combine(projectDirectory, "NServiceBus.TransportTests.csproj");
        var csproj = XDocument.Load(csprojPath);

        var removePatterns = csproj.Descendants("RemoveSourceFileFromPackage")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v != null)
            .ToList();

        var addFiles = csproj.Descendants("AddSourceFileToPackage")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v != null)
            .Select(NormalizePath)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        var projectFiles = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsObjOrBin(f))
            .Select(f => NormalizePath(Path.GetRelativePath(projectDirectory, f)))
            .ToList();

        var shippedFiles = projectFiles
            .Where(f => !IsRemoved(f, removePatterns))
            .OrderBy(f => f, StringComparer.Ordinal)
            .Concat(addFiles)
            .ToList();

        Approver.Verify(string.Join("\n", shippedFiles));
    }

    static string NormalizePath(string path) => path.Replace('\\', '/');

    static bool IsObjOrBin(string path)
    {
        var normalized = NormalizePath(path);
        return normalized.Contains("/obj/") || normalized.Contains("/bin/");
    }

    static bool IsRemoved(string filePath, List<string> removePatterns)
    {
        var normalizedFilePath = NormalizePath(filePath);

        foreach (var rawPattern in removePatterns)
        {
            var pattern = NormalizePath(rawPattern);

            if (pattern.Contains("**"))
            {
                if (Regex.IsMatch(normalizedFilePath, GlobToRegex(pattern), RegexOptions.CultureInvariant))
                {
                    return true;
                }
            }
            else if (Path.GetFileName(normalizedFilePath) == pattern || normalizedFilePath == pattern)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts an MSBuild glob pattern to a regex pattern.
    /// Supports ** (any path) and * (any filename segment).
    /// </summary>
    static string GlobToRegex(string glob)
    {
        var result = "";
        var i = 0;
        while (i < glob.Length)
        {
            if (i + 1 < glob.Length && glob[i] == '*' && glob[i + 1] == '*')
            {
                result += ".*";
                i += 2;
                if (i < glob.Length && glob[i] == '/')
                {
                    result += "/?";
                    i++;
                }
            }
            else if (glob[i] == '*')
            {
                result += "[^/]*";
                i++;
            }
            else if (".+$^{}()[]|".Contains(glob[i]))
            {
                result += "\\" + glob[i];
                i++;
            }
            else
            {
                result += glob[i];
                i++;
            }
        }

        return "^" + result + "$";
    }
}