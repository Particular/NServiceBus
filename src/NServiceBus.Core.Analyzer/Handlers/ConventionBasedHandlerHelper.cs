#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

static class ConventionBasedHandlerHelper
{
    public static bool IsConventionBasedHandlerType(INamedTypeSymbol classType, HandlerKnownTypes knownTypes)
    {
        for (var current = classType; current is not null; current = current.BaseType)
        {
            if (HasValidConventionBasedHandleMethods(current, knownTypes))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasValidConventionBasedHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes)
    {
        var interfaceMessageTypes = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var iface in classType.AllInterfaces)
        {
            if (iface.IsGenericType && HandlerConventions.IsHandlerInterface(iface.OriginalDefinition, knownTypes) &&
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

            if (!HandlerConventions.IsSupportedHandlerReturnType(method.ReturnType))
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

    public static bool IsConventionBasedHandlerWithBoundCancellationToken(IMethodSymbol method, Compilation compilation)
    {
        var handlerAttribute = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
        var messageHandlerContext = compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");
        var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

        if (method.MethodKind != MethodKind.Ordinary ||
            method.Name != "Handle" ||
            method.Parameters.Length < 3 ||
            method.ContainingType is null ||
            handlerAttribute is null ||
            messageHandlerContext is null ||
            cancellationTokenType is null ||
            !method.ContainingType.HasAttribute(handlerAttribute))
        {
            return false;
        }

        var secondParam = method.Parameters[1];
        if (!secondParam.Type.Equals(messageHandlerContext, SymbolEqualityComparer.IncludeNullability))
        {
            return false;
        }

        return method.Parameters.Skip(2)
            .Any(param => param.Type.Equals(cancellationTokenType, SymbolEqualityComparer.IncludeNullability));
    }
}