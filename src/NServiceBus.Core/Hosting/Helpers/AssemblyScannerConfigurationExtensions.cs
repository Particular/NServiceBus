namespace NServiceBus
{

    /// <summary>
    /// Contains extension methods to configure the <see cref="AssemblyScanner"/> behavior.
    /// </summary>
    public static class AssemblyScannerConfigurationExtensions
    {
        /// <summary>
        /// Configure the <see cref="AssemblyScanner"/>.
        /// </summary>
        public static AssemblyScannerConfiguration AssemblyScanner(this EndpointConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            return configuration.Settings.GetOrCreate<AssemblyScannerConfiguration>();
        }
    }
}