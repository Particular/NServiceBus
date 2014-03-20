namespace NServiceBus
{
    /// <summary>
    /// The semver version of NServiceBus
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "5.0")]
    public static class NServiceBusVersion
    {

        static NServiceBusVersion()
        {
            var assembly = typeof(NServiceBusVersion).Assembly;
            var gitFlowVersionInformationType = assembly.GetType("NServiceBus.GitVersionInformation", true);
            var fieldInfo = gitFlowVersionInformationType.GetField("AssemblyFileVersion");
            var assemblyFileVersion = System.Version.Parse((string)fieldInfo.GetValue(null));
            Version = assemblyFileVersion.ToString(3);
        }

        public static readonly string Version;

    }
}
