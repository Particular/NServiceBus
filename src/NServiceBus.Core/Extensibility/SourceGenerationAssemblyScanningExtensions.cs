#nullable enable

namespace NServiceBus;

using System;
using System.ComponentModel;

/// <summary>
/// Provides an extension method <see cref="SourceGenerationTypeDiscoveryOptions" /> that
/// disables runtime assembly scanning and registers required types at compile time instead.
/// </summary>
public static class SourceGenerationAssemblyScanningExtensions
{
    /// <summary>
    /// Disable runtime assembly scanning and register required types using source generation instead.
    /// Roslyn analyzers and source generators must be enabled to replace the implementation of this
    /// method at compile time, otherwise a <see cref="NotImplementedException" /> will be thrown.
    /// </summary>
    public static SourceGenerationTypeDiscoveryOptions UseSourceGeneratedTypeDiscovery(this EndpointConfiguration endpointConfiguration)
        => throw new NotImplementedException("You can't turn off analyzers / source generation and use this configuration method.");

    /// <summary>
    /// Auto-registers message handlers and sagas using source generation at compile time.
    /// Roslyn analyzers and source generators must be enabled to replace the implementation of this
    /// method at compile time, otherwise a <see cref="NotImplementedException" /> will be thrown.
    /// </summary>
    public static SourceGenerationTypeDiscoveryOptions RegisterHandlersAndSagas(this SourceGenerationTypeDiscoveryOptions options)
        => throw new NotImplementedException("You can't turn off analyzers / source generation and use this configuration method.");
}

/// <summary>
/// Provides options to auto-register message handlers and sagas using source generation.
/// </summary>
public sealed class SourceGenerationTypeDiscoveryOptions
{
    internal SourceGenerationTypeDiscoveryOptions(EndpointConfiguration endpointConfiguration)
        => Configuration = endpointConfiguration;

    /// <summary>
    /// Makes the <see cref="EndpointConfiguration" /> instance available for source generator.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EndpointConfiguration Configuration { get; init; }
}