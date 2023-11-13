#nullable enable

namespace NServiceBus;

using System;

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
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.Settings.GetOrCreate<AssemblyScannerConfiguration>();
    }
}