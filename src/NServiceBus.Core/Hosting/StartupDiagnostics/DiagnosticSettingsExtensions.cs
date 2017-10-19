namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// Provides an API to add startup diagnostics.
    /// </summary>
    public static class DiagnosticSettingsExtensions
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


        /// <summary>
        /// Configures a custom path where host diagnostics is written.
        /// </summary>
        /// <param name="config">Configuration object to extend.</param>
        /// <param name="path">The custom path to use.</param>
        public static void SetDiagnosticsPath(this EndpointConfiguration config, string path)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(path), path);

            PathChecker.ThrowForBadPath(path, "Diagnostics root path");

            config.GetSettings().Set(DiagnosticsPathKey, path);
        }
        internal const string DiagnosticsPathKey = "Diagnostics.RootPath";

        /// <summary>
        /// Allows full control over how diagnostics data is persisted.
        /// </summary>
        /// <param name="config">Configuration object to extend.</param>
        /// <param name="customDiagnosticsWriter">Func responsible for writing diagnostics data.</param>
        public static void CustomDiagnosticsWriter(this EndpointConfiguration config, Func<string, Task> customDiagnosticsWriter)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(customDiagnosticsWriter), customDiagnosticsWriter);

            config.Settings.Set("HostDiagnosticsWriter", customDiagnosticsWriter);
        }

        internal static bool TryGetCustomDiagnosticsWriter(this ReadOnlySettings settings, out Func<string, Task> customDiagnosticsWriter)
        {
            return settings.TryGet("HostDiagnosticsWriter", out customDiagnosticsWriter);
        }
    }
}