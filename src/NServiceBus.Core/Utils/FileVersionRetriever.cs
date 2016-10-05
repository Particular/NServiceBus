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
            if (!string.IsNullOrEmpty(type.Assembly.Location))
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(type.Assembly.Location);

                return new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart).ToString(3);
            }

            var customAttributes = type.Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);

            if (customAttributes.Length >= 1)
            {
                var fileVersion = (AssemblyFileVersionAttribute) customAttributes[0];
                Version version;
                if (Version.TryParse(fileVersion.Version, out version))
                {
                    return version.ToString(3);
                }
            }

            return type.Assembly.GetName().Version.ToString(3);
        }
    }
}