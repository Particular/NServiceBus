namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        /// <summary>
        /// Specifies the range of types that NServiceBus scans for handlers etc.
        /// </summary>
        internal static void TypesToScanInternal(this EndpointConfiguration configuration, IEnumerable<Type> typesToScan)
        {
            configuration.Settings.Get<AssemblyScanningComponent.Configuration>().UserProvidedTypes = typesToScan.ToList();
        }
    }
}