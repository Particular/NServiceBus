namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Diagnostics;

    public class FileVersionRetriever
    {
        public static string GetFileVersion(Type type)
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(type.Assembly.Location);

            //build a semver compliant version
            return new Version(fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart).ToString(3);
        }
    }
}
