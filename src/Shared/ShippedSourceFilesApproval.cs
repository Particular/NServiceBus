// Source packages (e.g. NServiceBus.PersistenceTests.Sources) ship all .cs files to downstream
// implementers by default via Particular.Packaging's IncludeSourceFilesInPackage. Files must be
// explicitly excluded via RemoveSourceFileFromPackage in the .csproj.
//
// This approval test helper ensures that adding a new .cs file to a source package project is a
// deliberate choice — either add RemoveSourceFileFromPackage to exclude it, or update the approved
// list to acknowledge it should be shipped.
//
// IMPORTANT: When shipping a new source package release, changes to the approved list propagate to
// downstream repositories. Any newly approved file that is not excluded may cause downstream test
// failures until those repositories are updated. Consider validating against a select set of
// downstream repositories before shipping.

#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;

/// <summary>
/// Verifies that the set of source files shipped in a source package matches an approved list.
/// When a new .cs file is added to the project, the test will fail until the file is either
/// excluded via RemoveSourceFileFromPackage or the approved list is updated.
/// </summary>
static class ShippedSourceFilesApproval
{
    public static string GetShippedFiles()
    {
        var projectDirectory = FindProjectDirectory();
        var csprojPath = FindCsproj(projectDirectory);
        var csproj = XDocument.Load(csprojPath);

        var removeEntries = csproj.Descendants("RemoveSourceFileFromPackage")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .Select(CompileRemovePattern!)
            .ToList()
            .AsReadOnly();

        var addFiles = csproj.Descendants("AddSourceFileToPackage")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .Select(NormalizePath!)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();

        var projectFiles = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsObjOrBin(f))
            .Select(f => NormalizePath(Path.GetRelativePath(projectDirectory, f)))
            .ToList();

        var shippedFiles = projectFiles
            .Where(f => !IsRemoved(f, removeEntries))
            .OrderBy(f => f, StringComparer.Ordinal)
            .Concat(addFiles)
            .ToList();

        return string.Join("\n", shippedFiles);
    }

    static string FindProjectDirectory()
    {
        var directory = TestContext.CurrentContext.TestDirectory;
        while (directory != null)
        {
            if (Directory.GetFiles(directory, "*.csproj").Length == 1)
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Could not find project directory containing a .csproj file.");
    }

    static string FindCsproj(string projectDirectory) =>
        Directory.GetFiles(projectDirectory, "*.csproj")[0];

    static string NormalizePath(string path) => path.Replace('\\', '/');

    static bool IsObjOrBin(string path)
    {
        var normalized = NormalizePath(path);
        return normalized.Contains("/obj/") || normalized.Contains("/bin/");
    }

    record RemoveEntry(string Pattern, Regex? Regex);

    static RemoveEntry CompileRemovePattern(string rawPattern)
    {
        var pattern = NormalizePath(rawPattern);
        return pattern.Contains('*') ? new RemoveEntry(pattern, new Regex(GlobToRegex(pattern), RegexOptions.CultureInvariant)) : new RemoveEntry(pattern, null);
    }

    static bool IsRemoved(string filePath, IReadOnlyCollection<RemoveEntry> removeEntries)
    {
        foreach (var entry in removeEntries)
        {
            if (entry.Regex != null)
            {
                if (entry.Regex.IsMatch(filePath))
                {
                    return true;
                }
            }
            else if (Path.GetFileName(filePath) == entry.Pattern || filePath == entry.Pattern)
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

        return $"^{result}$";
    }
}