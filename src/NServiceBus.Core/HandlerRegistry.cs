#nullable enable
namespace NServiceBus;

using System.ComponentModel;

/// <summary>
/// Registry for generated handler registration extensions.
/// </summary>
public sealed class HandlerRegistry(EndpointConfiguration configuration)
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EndpointConfiguration Configuration { get; } = configuration;
}
