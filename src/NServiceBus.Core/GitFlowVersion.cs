namespace NServiceBus
{
    using System;

    static class GitFlowVersion
    {
        static GitFlowVersion()
        {
            var assembly = typeof(GitFlowVersion).Assembly;
            var gitFlowVersionInformationType = assembly.GetType("GitVersionInformation", true);
            var fieldInfo = gitFlowVersionInformationType.GetField("MajorMinorPatch");

            var version = Version.Parse((string) fieldInfo.GetValue(null));

            MajorMinor = version.ToString(2);
            MajorMinorPatch = version.ToString(3);
        }

        public static string MajorMinor;
        public static string MajorMinorPatch;
    }
}