#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;

public sealed class MarkerTypes(Compilation compilation)
{
    readonly INamedTypeSymbol iMessage = compilation.GetTypeByMetadataName("NServiceBus.IMessage")!;
    readonly INamedTypeSymbol iCommand = compilation.GetTypeByMetadataName("NServiceBus.ICommand")!;
    readonly INamedTypeSymbol iEvent = compilation.GetTypeByMetadataName("NServiceBus.IEvent")!;

    public bool IsMarkerInterface(INamedTypeSymbol type) =>
        SymbolEqualityComparer.Default.Equals(type, iMessage) ||
        SymbolEqualityComparer.Default.Equals(type, iCommand) ||
        SymbolEqualityComparer.Default.Equals(type, iEvent);
}