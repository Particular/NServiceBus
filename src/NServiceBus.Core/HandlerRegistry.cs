#nullable enable
namespace NServiceBus;

using System.ComponentModel;

/// <summary>
/// Registry for generated handler registration extensions.
/// </summary>
/// <remarks>
/// <para>
/// When message handlers are decorated with the <see cref="HandlerAttribute" /> and sagas are decorated with the
/// <see cref="SagaAttribute" />, methods to register these handlers and sagas are generated here.
/// </para>
/// <para>
/// For more information, see the remarks for <see cref="HandlerAttribute" /> and <see cref="SagaAttribute" />.
/// </para>
/// </remarks>
public sealed class HandlerRegistry(EndpointConfiguration configuration)
{
    /// <summary>
    /// The endpoint configuration.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EndpointConfiguration Configuration { get; } = configuration;
}