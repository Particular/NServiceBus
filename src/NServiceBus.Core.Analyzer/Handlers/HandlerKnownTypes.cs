#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;

public readonly record struct HandlerKnownTypes(
    INamedTypeSymbol IHandleMessages,
    INamedTypeSymbol IHandleTimeouts,
    INamedTypeSymbol IAmStartedByMessages,
    INamedTypeSymbol HandlerAttribute,
    INamedTypeSymbol SagaBase,
    INamedTypeSymbol IMessageHandlerContext)
{
    public static bool TryGet(Compilation compilation, out HandlerKnownTypes knownTypes)
    {
        var iHandleMessages = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
        var iHandleTimeouts = compilation.GetTypeByMetadataName("NServiceBus.IHandleTimeouts`1");
        var iAmStartedByMessages = compilation.GetTypeByMetadataName("NServiceBus.IAmStartedByMessages`1");
        var handlerAttribute = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
        var sagaBase = compilation.GetTypeByMetadataName("NServiceBus.Saga`1");
        var iMessageHandlerContext = compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");

        if (iHandleMessages is null || iHandleTimeouts is null || iAmStartedByMessages is null ||
            handlerAttribute is null || sagaBase is null || iMessageHandlerContext is null)
        {
            knownTypes = default;
            return false;
        }

        knownTypes = new HandlerKnownTypes(iHandleMessages, iHandleTimeouts, iAmStartedByMessages, handlerAttribute, sagaBase, iMessageHandlerContext);
        return true;
    }
}