#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

static class FileVersionRetriever
{
    public static string GetFileVersion(Type type) => GetFileVersion(type.Assembly);

    [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "Location is checked for empty string before use; falls back to AssemblyFileVersionAttribute or assembly name version when running as a single-file app.")]
    public static string GetFileVersion(Assembly assembly)
    {
        if (!string.IsNullOrEmpty(assembly.Location))
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

            return new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart).ToString(3);
        }

        var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

        if (Version.TryParse(fileVersionAttribute?.Version, out var version))
        {
            return version.ToString(3);
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}