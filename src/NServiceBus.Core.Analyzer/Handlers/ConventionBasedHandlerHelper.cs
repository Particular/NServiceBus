#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

static class ConventionBasedHandlerHelper
{
    public const string HandleMethodName = "Handle";

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

            if (IsValidConventionBasedHandleMethod(method, knownTypes, interfaceMessageTypes))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsValidConventionBasedHandleMethod(IMethodSymbol method, HandlerKnownTypes knownTypes, HashSet<string> interfaceMessageTypes)
    {
        if (method.Name != HandleMethodName ||
            method.DeclaredAccessibility != Accessibility.Public ||
            method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
            method.IsAbstract ||
            method.IsGenericMethod ||
            method.IsExtensionMethod ||
            method.IsVirtual ||
            method.ReturnsVoid ||
            method.Parameters.Length < 2)
        {
            return false;
        }

        if (method.Parameters[0].Type is not INamedTypeSymbol messageType)
        {
            return false;
        }

        // Second param must be IMessageHandlerContext
        var secondParam = method.Parameters[1];
        if (!SymbolEqualityComparer.Default.Equals(secondParam.Type.OriginalDefinition, knownTypes.IMessageHandlerContext) &&
            !SymbolEqualityComparer.Default.Equals(secondParam.Type, knownTypes.IMessageHandlerContext))
        {
            return false;
        }

        if (!HandlerConventions.IsSupportedHandlerReturnType(method.ReturnType))
        {
            return false;
        }

        // If the class implements IHandleMessages<T> for this exact message type with exactly 2 params,
        // this Handle method is the interface implementation — not a convention-based method.
        var messageTypeFqn = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (method.Parameters.Length == 2 && interfaceMessageTypes.Contains(messageTypeFqn))
        {
            return false;
        }
        return true;
    }

    public static bool IsConventionBasedHandlerWithBoundCancellationToken(IMethodSymbol method, Compilation compilation)
    {
        var handlerAttribute = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
        var messageHandlerContext = compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");
        var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

        if (method.MethodKind != MethodKind.Ordinary ||
            method.Name != HandleMethodName ||
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

    public static ConstructorAnalysis AnalyzeConstructors(INamedTypeSymbol classType, INamedTypeSymbol? activatorUtilitiesConstructorAttributeType)
    {
        var candidates = classType.Constructors
            .Where(ctor => !ctor.IsStatic && ctor.DeclaredAccessibility != Accessibility.Private)
            .ToArray();

        if (candidates.Length == 0)
        {
            return new ConstructorAnalysis(HasAccessibleConstructor: false, AmbiguousParameterCount: null);
        }

        // If any constructor has [ActivatorUtilitiesConstructor], use that set
        if (activatorUtilitiesConstructorAttributeType is not null)
        {
            var markedConstructors = candidates
                .Where(ctor => HasAttribute(ctor, activatorUtilitiesConstructorAttributeType))
                .ToArray();

            if (markedConstructors.Length > 0)
            {
                candidates = markedConstructors;
            }
        }

        // Find the maximum parameter count
        var maxParamCount = candidates.Max(ctor => ctor.Parameters.Length);

        // Check if multiple constructors have this max count
        var constructorsWithMaxCount = candidates
            .Where(ctor => ctor.Parameters.Length == maxParamCount)
            .ToArray();

        return new ConstructorAnalysis(
            HasAccessibleConstructor: true,
            AmbiguousParameterCount: constructorsWithMaxCount.Length > 1 ? maxParamCount : null);
    }

    public readonly record struct ConstructorAnalysis(
        bool HasAccessibleConstructor,
        int? AmbiguousParameterCount);

    static bool HasAttribute(IMethodSymbol method, INamedTypeSymbol attributeType)
    {
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not null &&
                (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType) ||
                 SymbolEqualityComparer.Default.Equals(attr.AttributeClass.OriginalDefinition, attributeType)))
            {
                return true;
            }
        }
        return false;
    }
}