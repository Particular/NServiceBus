namespace NServiceBus
{
    using Settings;

    //internal for now
    static class EndpointDiagnosticSettingsExtensions
    {
        public static void AddStartupDiagnosticsSection(this ReadOnlySettings settings, string sectionName, object section)
        {
            Guard.AgainstNull(nameof(settings), settings);
            Guard.AgainstNullAndEmpty(nameof(sectionName), sectionName);
            Guard.AgainstNull(nameof(section), section);

            settings.Get<StartupDiagnosticEntries>().Add(sectionName, section);
        }
    }
}