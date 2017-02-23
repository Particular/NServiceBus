namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to configure the <see cref="AssemblyScanner"/> behavior.
    /// </summary>
    public static class AssemblyScannerConfigurationExtensions
    {
        /// <summary>
        /// Configure the <see cref="AssemblyScanner"/>.
        /// </summary>
        public static void AssemblyScanner(this EndpointConfiguration configuration, Action<AssemblyScannerConfiguration> configure)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(configure), configure);

            var config = configuration.Settings.GetOrCreate<AssemblyScannerConfiguration>();
            configure(config);
        }
    }
}