namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Helper class to retrieve File version.
    /// </summary>
    class FileVersionRetriever
    {
        /// <summary>
        /// Retrieves a semver compliant version from a <see cref="Type" />.
        /// </summary>
        /// <param name="type"><see cref="Type" /> to retrieve version from.</param>
        /// <returns>SemVer compliant version.</returns>
        public static string GetFileVersion(Type type)
        {
            var assembly = type.Assembly;
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

                return new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart).ToString(3);
            }

            var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

            if (Version.TryParse(fileVersionAttribute.Version, out var version))
            {
                return version.ToString(3);
            }

            return assembly.GetName().Version.ToString(3);
        }
    }
}