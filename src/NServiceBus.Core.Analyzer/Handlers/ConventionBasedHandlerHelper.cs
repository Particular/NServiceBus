#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class ConventionBasedHandlerHelper
{
    public const string HandleMethodName = "Handle";

    public static bool IsConventionBasedHandlerType(INamedTypeSymbol classType, HandlerKnownTypes knownTypes, Compilation? compilation = null)
    {
        for (var current = classType; current is not null; current = current.BaseType)
        {
            if (HasValidConventionBasedHandleMethods(current, knownTypes, classType, compilation))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasValidConventionBasedHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes, Compilation? compilation = null) =>
        HasValidConventionBasedHandleMethods(classType, knownTypes, classType, compilation);

    static bool HasValidConventionBasedHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes, INamedTypeSymbol interfaceImplementationType, Compilation? compilation = null)
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

            if (IsValidConventionBasedHandleMethod(method, knownTypes, interfaceMessageTypes, interfaceImplementationType, compilation))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsValidConventionBasedHandleMethod(IMethodSymbol method, HandlerKnownTypes knownTypes, HashSet<string> interfaceMessageTypes, INamedTypeSymbol? interfaceImplementationType = null, Compilation? compilation = null)
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

        // If this Handle method is the implementation of an interface member that belongs to a handler
        // interface hierarchy (IHandleMessages<T>, IHandleTimeouts<T>, IAmStartedByMessages<T>, or an
        // interface deriving from them), it is interface-based, not convention-based.
        // Unrelated interfaces with a Handle-like method do NOT disqualify a convention-based handler.
        if (ImplementsHandlerInterfaceMember(method, knownTypes, interfaceImplementationType))
        {
            return false;
        }

        // If this Handle method is called from within an interface-implementing Handle method
        // on the same class, it is a helper, not a convention-based handler.
        if (compilation is not null && interfaceImplementationType is not null &&
            IsCalledFromInterfaceHandlerMethod(method, interfaceImplementationType, knownTypes, compilation))
        {
            return false;
        }

        return true;
    }

    // If a Handle method is invoked from within any IHandleMessages<T>.Handle implementation
    // on the same class, it is an internal helper, not a convention-based handler.
    static bool IsCalledFromInterfaceHandlerMethod(
        IMethodSymbol suspectMethod,
        INamedTypeSymbol classType,
        HandlerKnownTypes knownTypes,
        Compilation compilation)
    {
        foreach (var iface in classType.AllInterfaces)
        {
            if (!iface.IsGenericType)
            {
                continue;
            }

            if (!HandlerConventions.IsHandlerInterface(iface.OriginalDefinition, knownTypes))
            {
                continue;
            }

            foreach (var ifaceMethod in iface.GetMembers("Handle").OfType<IMethodSymbol>())
            {
                var impl = classType.FindImplementationForInterfaceMember(ifaceMethod);
                if (impl is null || SymbolEqualityComparer.Default.Equals(impl, suspectMethod))
                {
                    continue;
                }

                foreach (var syntaxRef in impl.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxRef.GetSyntax();
                    var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

                    foreach (var invocation in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                        if (symbolInfo.Symbol is IMethodSymbol invokedMethod &&
                            SymbolEqualityComparer.Default.Equals(invokedMethod, suspectMethod))
                        {
                            return true;
                        }

                        if (symbolInfo.Symbol is IMethodSymbol { ReducedFrom: { } reduced } &&
                            SymbolEqualityComparer.Default.Equals(reduced, suspectMethod))
                        {
                            return true;
                        }
                    }
                }
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