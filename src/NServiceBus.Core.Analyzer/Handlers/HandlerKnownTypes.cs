#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;

public readonly record struct HandlerKnownTypes(
    INamedTypeSymbol IHandleMessages,
    INamedTypeSymbol HandlerAttribute,
    INamedTypeSymbol SagaBase,
    INamedTypeSymbol IMessageHandlerContext)
{
    public static bool TryGet(Compilation compilation, out HandlerKnownTypes knownTypes)
    {
        var iHandleMessages = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
        var handlerAttribute = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
        var sagaBase = compilation.GetTypeByMetadataName("NServiceBus.Saga`1");
        var iMessageHandlerContext = compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");

        if (iHandleMessages is null || handlerAttribute is null || sagaBase is null || iMessageHandlerContext is null)
        {
            knownTypes = default;
            return false;
        }

        knownTypes = new HandlerKnownTypes(iHandleMessages, handlerAttribute, sagaBase, iMessageHandlerContext);
        return true;
    }
}