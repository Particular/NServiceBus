#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

public sealed record HandlerKnownTypes(
    MarkerTypes MarkerTypes,
    INamedTypeSymbol IHandleMessages,
    INamedTypeSymbol IHandleTimeouts,
    INamedTypeSymbol IAmStartedByMessages,
    INamedTypeSymbol HandlerAttribute,
    INamedTypeSymbol SagaBase,
    INamedTypeSymbol IMessageHandlerContext,
    INamedTypeSymbol? CancellationTokenType,
    INamedTypeSymbol? ActivatorUtilitiesConstructorAttributeType)
{
    public static bool TryGet(Compilation compilation, [NotNullWhen(true)] out HandlerKnownTypes? knownTypes)
    {
        var markerTypes = new MarkerTypes(compilation);
        var iHandleMessages = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
        var iHandleTimeouts = compilation.GetTypeByMetadataName("NServiceBus.IHandleTimeouts`1");
        var iAmStartedByMessages = compilation.GetTypeByMetadataName("NServiceBus.IAmStartedByMessages`1");
        var handlerAttribute = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
        var sagaBase = compilation.GetTypeByMetadataName("NServiceBus.Saga`1");
        var iMessageHandlerContext = compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");
        var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        var activatorUtilitiesConstructorAttributeType = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructorAttribute");

        if (iHandleMessages is null || iHandleTimeouts is null || iAmStartedByMessages is null ||
            handlerAttribute is null || sagaBase is null || iMessageHandlerContext is null)
        {
            knownTypes = default;
            return false;
        }

        knownTypes = new HandlerKnownTypes(
            markerTypes,
            iHandleMessages,
            iHandleTimeouts,
            iAmStartedByMessages,
            handlerAttribute,
            sagaBase,
            iMessageHandlerContext,
            cancellationTokenType,
            activatorUtilitiesConstructorAttributeType);
        return true;
    }
}