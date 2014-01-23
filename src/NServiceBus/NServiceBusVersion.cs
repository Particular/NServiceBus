namespace NServiceBus
{
    /// <summary>
    /// The semver version of NServiceBus
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "5.0")]
    public static class NServiceBusVersion
    {
        /// <summary>
        /// The semver version of NServiceBus
        /// </summary>
        public static string Version = GitFlowVersion.MajorMinorPatch;
    }
}
