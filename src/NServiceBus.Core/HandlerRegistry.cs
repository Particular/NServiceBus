#nullable enable
namespace NServiceBus;

using System;
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

    /// <summary>
    /// Registers all handlers and sagas of this assembly and of every referenced assembly that has a generated handler registry.
    /// </summary>
    /// <remarks>
    /// The NServiceBus source generator replaces calls to this method at compile time. The call throws when the generator did not run,
    /// for example in projects that do not use C#.
    /// </remarks>
    public void AddAll() => throw NotIntercepted(nameof(AddAll));

    /// <summary>
    /// Registers all handlers of this assembly and of every referenced assembly that has a generated handler registry.
    /// </summary>
    /// <inheritdoc cref="AddAll" path="/remarks" />
    public void AddAllHandlers() => throw NotIntercepted(nameof(AddAllHandlers));

    /// <summary>
    /// Registers all sagas of this assembly and of every referenced assembly that has a generated handler registry.
    /// </summary>
    /// <inheritdoc cref="AddAll" path="/remarks" />
    public void AddAllSagas() => throw NotIntercepted(nameof(AddAllSagas));

    static InvalidOperationException NotIntercepted(string methodName) =>
        new($"{nameof(HandlerRegistry)}.{methodName}() was not replaced by the NServiceBus source generator, which only runs in C# projects that reference the NServiceBus package with analyzers enabled. Keep assembly scanning enabled, or register each assembly with endpointConfiguration.Handlers.<AssemblyName>Assembly.{methodName}().");
}
