namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;

    /// <summary>
    /// Provides diagnostics configuration options.
    /// </summary>
    public static class HostDiagnosticsConfigurationExtensions
    {
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

        /// <summary>
        /// Allows full control over how diagnostics data is persisted.
        /// </summary>
        /// <param name="config">Configuration object to extend.</param>
        /// <param name="customDiagnosticsWriter">Func responsible for writing diagnostics data.</param>
        public static void CustomDiagnosticsWriter(this EndpointConfiguration config, Func<string, Task> customDiagnosticsWriter)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(customDiagnosticsWriter), customDiagnosticsWriter);

            config.Settings.Set<HostDiagnosticsWriter>(new HostDiagnosticsWriter(customDiagnosticsWriter));
        }

        internal const string DiagnosticsPathKey = "Diagnostics.RootPath";
    }
}