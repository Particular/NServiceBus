#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;

static class HandlerConventions
{
    public static bool IsHandlerInterface(INamedTypeSymbol ifaceDefinition, HandlerKnownTypes knownTypes) =>
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IHandleMessages) ||
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IHandleTimeouts) ||
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IAmStartedByMessages);

    public static bool IsHandlerInterface(INamedTypeSymbol type) => type is
    {
        // Handling IAmStartedByMessage is not ideal, but it avoids us having to do extensive semantic analysis on the sagas
        Name: "IHandleMessages" or "IHandleTimeouts" or "IAmStartedByMessages",
        IsGenericType: true,
        ContainingNamespace:
        {
            Name: "NServiceBus",
            ContainingNamespace.IsGlobalNamespace: true
        }
    };

    public static bool IsSupportedHandlerReturnType(ITypeSymbol type) =>
        type is INamedTypeSymbol
        {
            Name: "Task",
            ContainingNamespace:
            {
                Name: "Tasks",
                ContainingNamespace:
                {
                    Name: "Threading",
                    ContainingNamespace:
                    {
                        Name: "System",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }
            }
        };
}