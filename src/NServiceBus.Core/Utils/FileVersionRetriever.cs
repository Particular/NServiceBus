namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    class FileVersionRetriever
    {
        public static string GetFileVersion(Type type) => GetFileVersion(type.Assembly);

        public static string GetFileVersion(Assembly assembly)
        {
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