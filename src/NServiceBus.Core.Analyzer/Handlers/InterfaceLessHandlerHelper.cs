#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

static class InterfaceLessHandlerHelper
{
    public static bool IsInterfaceLessHandlerType(INamedTypeSymbol classType, HandlerKnownTypes knownTypes)
    {
        for (var current = classType; current is not null; current = current.BaseType)
        {
            if (HasValidInterfaceLessHandleMethods(current, knownTypes))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasValidInterfaceLessHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes)
    {
        var interfaceMessageTypes = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var iface in classType.AllInterfaces)
        {
            if (iface.IsGenericType &&
                IsHandlerInterface(iface.OriginalDefinition, knownTypes) &&
                iface.TypeArguments[0] is INamedTypeSymbol msgType)
            {
                interfaceMessageTypes.Add(msgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        foreach (var member in classType.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            if (method.Name != "Handle" ||
                method.DeclaredAccessibility != Accessibility.Public ||
                method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
                method.Parameters.Length < 2)
            {
                continue;
            }

            if (method.Parameters[0].Type is not INamedTypeSymbol)
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, knownTypes.IMessageHandlerContext))
            {
                continue;
            }

            if (!IsSupportedHandlerReturnType(method.ReturnType))
            {
                continue;
            }

            var firstParamFqn = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (method.Parameters.Length == 2 && interfaceMessageTypes.Contains(firstParamFqn))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    static bool IsHandlerInterface(INamedTypeSymbol ifaceDefinition, HandlerKnownTypes knownTypes) =>
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IHandleMessages) ||
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IHandleTimeouts) ||
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IAmStartedByMessages);

    static bool IsSupportedHandlerReturnType(ITypeSymbol type) =>
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