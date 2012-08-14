namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Helper class to retrieve File version.
    /// </summary>
    public class FileVersionRetriever
    {
        /// <summary>
        /// Retrieves a semver compiant version from a <see cref="Type"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to retrieve version from.</param>
        /// <returns>SemVer compiant version.</returns>
        public static string GetFileVersion(Type type)
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(type.Assembly.Location);

            //build a semver compliant version
            return new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart).ToString(3);
        }
    }
}
