#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class ConventionBasedHandlerHelper
{
    public const string HandleMethodName = "Handle";

    public static bool IsConventionBasedHandlerType(INamedTypeSymbol classType, HandlerKnownTypes knownTypes, Compilation? compilation = null, CancellationToken cancellationToken = default)
    {
        var interfaceMessageTypes = CollectInterfaceMessageTypes(classType, knownTypes);
        var interfaceHandleCallees = compilation is null
            ? null
            : CollectInterfaceHandleCallees(classType, knownTypes, compilation, cancellationToken);

        for (var current = classType; current is not null; current = current.BaseType)
        {
            if (HasValidConventionBasedHandleMethods(current, knownTypes, interfaceMessageTypes, interfaceHandleCallees))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasValidConventionBasedHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes, Compilation? compilation = null, CancellationToken cancellationToken = default) =>
        HasValidConventionBasedHandleMethods(classType, knownTypes,
            CollectInterfaceMessageTypes(classType, knownTypes),
            compilation is null ? null : CollectInterfaceHandleCallees(classType, knownTypes, compilation, cancellationToken));

    static bool HasValidConventionBasedHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes, HashSet<ITypeSymbol> interfaceMessageTypes, HashSet<IMethodSymbol>? interfaceHandleCallees)
    {
        foreach (var member in classType.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            if (!IsValidConventionBasedHandleMethod(method, knownTypes, interfaceMessageTypes))
            {
                continue;
            }

            // Exclude methods that are helpers called from within an interface Handle implementation
            if (interfaceHandleCallees is not null &&
                (interfaceHandleCallees.Contains(method) || interfaceHandleCallees.Contains(method.OriginalDefinition)))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public static bool IsValidConventionBasedHandleMethod(IMethodSymbol method, HandlerKnownTypes knownTypes, HashSet<ITypeSymbol> interfaceMessageTypes, INamedTypeSymbol? interfaceImplementationType = null)
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
        // this Handle method is the interface implementation, not a convention-based method.
        if (method.Parameters.Length == 2 && interfaceMessageTypes.Contains(messageType))
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

    static HashSet<ITypeSymbol> CollectInterfaceMessageTypes(INamedTypeSymbol classType, HandlerKnownTypes knownTypes)
    {
        var messageTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var iface in classType.AllInterfaces)
        {
            if (iface.IsGenericType && HandlerConventions.IsHandlerInterface(iface.OriginalDefinition, knownTypes) &&
                iface.TypeArguments[0] is INamedTypeSymbol msgType)
            {
                messageTypes.Add(msgType);
            }
        }

        return messageTypes;
    }

    // Collects all methods that are called from within interface Handle method implementations.
    // These are helper methods, not convention-based handlers.
    static HashSet<IMethodSymbol> CollectInterfaceHandleCallees(
        INamedTypeSymbol classType,
        HandlerKnownTypes knownTypes,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var visitedImplementations = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var callees = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        SyntaxTree? cachedTree = null;
        SemanticModel? cachedModel = null;

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

            foreach (var member in iface.GetMembers(HandleMethodName))
            {
                if (member is not IMethodSymbol ifaceMethod)
                {
                    continue;
                }

                if (classType.FindImplementationForInterfaceMember(ifaceMethod) is not IMethodSymbol impl)
                {
                    continue;
                }

                if (!visitedImplementations.Add(impl))
                {
                    continue;
                }

                foreach (var syntaxRef in impl.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxRef.GetSyntax(cancellationToken);

                    if (!ReferenceEquals(syntax.SyntaxTree, cachedTree))
                    {
                        cachedTree = syntax.SyntaxTree;
                        cachedModel = compilation.GetSemanticModel(cachedTree);
                    }

                    foreach (var invocation in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (cachedModel!.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol invokedMethod)
                        {
                            continue;
                        }

                        callees.Add(invokedMethod);
                        callees.Add(invokedMethod.OriginalDefinition);

                        if (invokedMethod.ReducedFrom is { } reduced)
                        {
                            callees.Add(reduced);
                            callees.Add(reduced.OriginalDefinition);
                        }
                    }
                }
            }
        }

        return callees;
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

            foreach (var member in iface.GetMembers(method.Name))
            {
                if (member is not IMethodSymbol interfaceMethod)
                {
                    continue;
                }

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