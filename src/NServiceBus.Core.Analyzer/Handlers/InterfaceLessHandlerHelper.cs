#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

static class InterfaceLessHandlerHelper
{
    public static bool IsInterfaceLessHandlerType(INamedTypeSymbol classType, INamedTypeSymbol iMessageHandlerContext)
    {
        for (var current = classType; current is not null; current = current.BaseType)
        {
            if (HasValidInterfaceLessHandleMethods(current, iMessageHandlerContext))
            {
                return true;
            }
        }

        return false;
    }

    static bool HasValidInterfaceLessHandleMethods(INamedTypeSymbol classType, INamedTypeSymbol iMessageHandlerContext)
    {
        var interfaceMessageTypes = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var iface in classType.AllInterfaces)
        {
            if (iface is { Name: "IHandleMessages" or "IHandleTimeouts" or "IAmStartedByMessages", IsGenericType: true } &&
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

            if (!IsIMessageHandlerContext(method.Parameters[1].Type, iMessageHandlerContext))
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

    public static bool IsIMessageHandlerContext(ITypeSymbol type, INamedTypeSymbol iMessageHandlerContext) =>
        SymbolEqualityComparer.Default.Equals(type, iMessageHandlerContext);

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