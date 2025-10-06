namespace NServiceBus;

using System;

/// <summary>
/// Provides an extension method <see cref="TurnOffAssemblyScanningAndUseSourceGenerationInstead" /> that
/// disables runtime assembly scanning and registers required types at compile time instead.
/// </summary>
public static class SourceGenerationAssemblyScanningExtensions
{
    /// <summary>
    /// Disable runtime assembly scanning and register required types using source generation instead.
    /// Roslyn analyzers and source generators must be enabled to replace the implementation of this
    /// method at compile time, otherwise a <see cref="NotImplementedException" /> will be thrown.
    /// </summary>
    public static void TurnOffAssemblyScanningAndUseSourceGenerationInstead(this EndpointConfiguration endpointConfiguration, bool autoRegisterHandlers)
        => throw new NotImplementedException("You can't turn off analyzers / source generation and use this configuration method.");
}