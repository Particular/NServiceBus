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
            if (HasValidConventionBasedHandleMethods(current, knownTypes, classType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasValidConventionBasedHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes) =>
        HasValidConventionBasedHandleMethods(classType, knownTypes, classType);

    static bool HasValidConventionBasedHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes, INamedTypeSymbol interfaceImplementationType)
    {
        var interfaceMessageTypes = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var iface in interfaceImplementationType.AllInterfaces)
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

            if (IsValidConventionBasedHandleMethod(method, knownTypes, interfaceMessageTypes, interfaceImplementationType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsValidConventionBasedHandleMethod(IMethodSymbol method, HandlerKnownTypes knownTypes, HashSet<string> interfaceMessageTypes, INamedTypeSymbol? interfaceImplementationType = null)
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

        if (method.Parameters[0].Type is not INamedTypeSymbol messageType ||
            !IsPlausibleMessageType(messageType))
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

        // If this Handle method is the implementation of an interface member that belongs to a handler
        // interface hierarchy (IHandleMessages<T>, IHandleTimeouts<T>, IAmStartedByMessages<T>, or an
        // interface deriving from them), it is interface-based, not convention-based.
        // Unrelated interfaces with a Handle-like method do NOT disqualify a convention-based handler.
        if (ImplementsHandlerInterfaceMember(method, knownTypes, interfaceImplementationType))
        {
            return false;
        }

        return true;
    }

    // NServiceBus messages are user-defined types. Framework/system types (string, primitives,
    // object, Guid, Uri, etc.) and value types can never be messages, so a Handle method whose
    // first parameter is such a type is an unrelated helper overload, not a convention-based handler.
    static bool IsPlausibleMessageType(INamedTypeSymbol type)
    {
        if (type.SpecialType != SpecialType.None)
        {
            return false;
        }

        if (type.TypeKind is not (TypeKind.Class or TypeKind.Interface))
        {
            return false;
        }

        return !IsInSystemNamespace(type);
    }

    static bool IsInSystemNamespace(ITypeSymbol type)
    {
        for (var ns = type.ContainingNamespace; ns is { IsGlobalNamespace: false }; ns = ns.ContainingNamespace)
        {
            if (ns.ContainingNamespace is { IsGlobalNamespace: true })
            {
                return ns.Name == "System";
            }
        }

        return false;
    }

    static bool ImplementsHandlerInterfaceMember(IMethodSymbol method, HandlerKnownTypes knownTypes, INamedTypeSymbol? interfaceImplementationType)
    {
        // Fast path: check explicit interface implementations first (O(1))
        if (!method.ExplicitInterfaceImplementations.IsEmpty)
        {
            // Only count as handler interface member if the explicit implementation targets a handler interface
            foreach (var explicitImpl in method.ExplicitInterfaceImplementations)
            {
                if (IsHandlerInterfaceOrDerived(explicitImpl.ContainingType, knownTypes))
                {
                    return true;
                }
            }

            return false;
        }

        interfaceImplementationType ??= method.ContainingType;
        if (interfaceImplementationType is null)
        {
            return false;
        }

        // Check implicit interface implementations — only for handler interface hierarchies
        foreach (var iface in interfaceImplementationType.AllInterfaces)
        {
            if (!IsHandlerInterfaceOrDerived(iface, knownTypes))
            {
                continue;
            }

            foreach (var interfaceMethod in iface.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                if (interfaceMethod.Parameters.Length != method.Parameters.Length)
                {
                    continue;
                }

                var implementation = interfaceImplementationType.FindImplementationForInterfaceMember(interfaceMethod);
                if (SymbolEqualityComparer.Default.Equals(implementation, method))
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool IsHandlerInterfaceOrDerived(INamedTypeSymbol iface, HandlerKnownTypes knownTypes)
    {
        // Direct check: is this one of the known handler interface definitions?
        if (iface.IsGenericType && HandlerConventions.IsHandlerInterface(iface.OriginalDefinition, knownTypes))
        {
            return true;
        }

        // Derived check: does this interface inherit from a handler interface?
        foreach (var baseInterface in iface.AllInterfaces)
        {
            if (baseInterface.IsGenericType && HandlerConventions.IsHandlerInterface(baseInterface.OriginalDefinition, knownTypes))
            {
                return true;
            }
        }

        return false;
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