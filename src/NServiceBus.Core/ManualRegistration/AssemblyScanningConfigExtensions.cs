namespace NServiceBus;

using System;

/// <summary>
/// Extensions for configuring type registration mode.
/// </summary>
public static class RegistrationModeConfigExtensions
{
    /// <summary>
    /// Sets the registration mode for the endpoint, controlling how types (handlers, messages, sagas, etc.)
    /// are discovered and registered.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    /// <param name="mode">The <see cref="RegistrationMode"/> to use.</param>
    /// <remarks>
    /// <para>
    /// <see cref="RegistrationMode.AssemblyScanning"/> (default): NServiceBus automatically discovers types via reflection.
    /// </para>
    /// <para>
    /// <see cref="RegistrationMode.Manual"/>: Assembly scanning is disabled. All types must be registered explicitly
    /// using Register* methods (e.g., RegisterHandler, RegisterMessage). Required for AOT compilation and
    /// enables multiple independent endpoints in a single process.
    /// </para>
    /// <para>
    /// <see cref="RegistrationMode.SourceGenerated"/>: Assembly scanning is disabled. Types are registered via
    /// code generated at compile time. Provides the same benefits as Manual mode with automatic discovery at build time.
    /// </para>
    /// </remarks>
    public static void UseRegistrationMode(this EndpointConfiguration config, RegistrationMode mode)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.Settings.Set(mode);

        // When not using AssemblyScanning, set UserProvidedTypes to empty to disable directory scanning
        if (mode != RegistrationMode.AssemblyScanning)
        {
            config.TypesToScanInternal(Array.Empty<Type>());
        }
    }

    /// <summary>
    /// Gets the current registration mode for the endpoint.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance.</param>
    /// <returns>The current <see cref="RegistrationMode"/>. Returns <see cref="RegistrationMode.AssemblyScanning"/> if not explicitly set.</returns>
    public static RegistrationMode GetRegistrationMode(this EndpointConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.Settings.TryGet(out RegistrationMode mode) 
            ? mode 
            : RegistrationMode.AssemblyScanning;
    }
}


