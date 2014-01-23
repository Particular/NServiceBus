namespace NServiceBus
{
    using System;

    static class GitFlowVersion
    {
        static GitFlowVersion()
        {
            var assembly = typeof(GitFlowVersion).Assembly;
            var gitFlowVersionInformationType = assembly.GetType("NServiceBus.GitFlowVersionInformation", true);
            var fieldInfo = gitFlowVersionInformationType.GetField("AssemblyFileVersion");
            var assemblyFileVersion = Version.Parse((string)fieldInfo.GetValue(null));
            MajorMinor = assemblyFileVersion.ToString(2);
            MajorMinorPatch = assemblyFileVersion.ToString(3);
        }

        public static string MajorMinor;
        public static string MajorMinorPatch;
    }
}