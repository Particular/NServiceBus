namespace NServiceBus
{
    using Settings;

    /// <summary>
    /// Provides a API to add startup diagnostics.
    /// </summary>
    static class EndpointDiagnosticSettingsExtensions
    {
        /// <summary>
        /// Adds a section to the startup diagnostics.
        /// </summary>
        public static void AddStartupDiagnosticsSection(this ReadOnlySettings settings, string sectionName, object section)
        {
            Guard.AgainstNull(nameof(settings), settings);
            Guard.AgainstNullAndEmpty(nameof(sectionName), sectionName);
            Guard.AgainstNull(nameof(section), section);

            settings.Get<StartupDiagnosticEntries>().Add(sectionName, section);
        }
    }
}