#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Utility;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;

public static partial class Handlers
{
    public sealed record HandlerSpec : BaseParser.BaseSpec
    {
        public HandlerSpec(BaseParser.BaseSpec handlerBaseSpec,
            ImmutableEquatableArray<RegistrationSpec> registrations,
            ImmutableEquatableArray<ConventionBasedMethodSpec> conventionBasedMethods,
            bool isMixed) : base(handlerBaseSpec)
        {
            Registrations = registrations;
            ConventionBasedMethods = conventionBasedMethods;
            IsMixed = isMixed;
        }

        public ImmutableEquatableArray<RegistrationSpec> Registrations { get; }
        public ImmutableEquatableArray<ConventionBasedMethodSpec> ConventionBasedMethods { get; }
        public bool IsMixed { get; }
    }

    public readonly record struct HandlerSpecs(ImmutableEquatableArray<HandlerSpec> Handlers);

    public enum RegistrationType
    {
        MessageHandler,
        StartMessageHandler,
        TimeoutHandler,
    }

    public readonly record struct RegistrationSpec(RegistrationType RegistrationType, string MessageType, ImmutableEquatableArray<string> MessageHierarchy, string HandlerType);

    public readonly record struct InjectedParamSpec(string ParameterName, string FullyQualifiedType, bool IsCancellationToken);

    public readonly record struct ConventionBasedMethodSpec(
        string MessageType,
        ImmutableEquatableArray<string> MessageHierarchy,
        bool IsStatic,
        string HandlerType,
        ImmutableEquatableArray<InjectedParamSpec> CtorParams,
        ImmutableEquatableArray<InjectedParamSpec> MethodParams,
        string AdapterName);

    public static class Parser
    {
        public static HandlerSpec Parse(SemanticModel semanticModel, INamedTypeSymbol handlerType, BaseParser.SpecKind specKind, CancellationToken cancellationToken = default)
        {
            var baseHandlerSpec = BaseParser.Parse(handlerType, specKind, cancellationToken: cancellationToken);
            var markers = new MarkerTypes(semanticModel.Compilation);

            var allRegistrations = new List<RegistrationSpec>();
            var startedMessageTypes = new HashSet<string>();

            bool isInterfaceBased = false;
            foreach (var @interface in handlerType.AllInterfaces.Where(IsHandlerInterface))
            {
                cancellationToken.ThrowIfCancellationRequested();
                isInterfaceBased = true;

                if (@interface.TypeArguments[0] is not INamedTypeSymbol messageType)
                {
                    continue;
                }

                var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                RegistrationType? registrationType = @interface.Name switch
                {
                    "IHandleMessages" => RegistrationType.MessageHandler,
                    "IHandleTimeouts" => RegistrationType.TimeoutHandler,
                    "IAmStartedByMessages" => RegistrationType.StartMessageHandler,
                    _ => null,
                };

                if (!registrationType.HasValue)
                {
                    continue;
                }

                var hierarchy = new ImmutableEquatableArray<string>(GetTypeHierarchy(messageType, markers).Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                var spec = new RegistrationSpec(registrationType.Value, messageTypeName, hierarchy, baseHandlerSpec.FullyQualifiedName);
                allRegistrations.Add(spec);

                if (registrationType == RegistrationType.StartMessageHandler)
                {
                    startedMessageTypes.Add(spec.MessageType);
                }
            }

            // If a message type has a StartMessageHandler, drop the plain MessageHandler
            // but keep TimeoutHandler for that message.
            var registrations = allRegistrations
                .Where(r =>
                    r.RegistrationType != RegistrationType.MessageHandler ||
                    !startedMessageTypes.Contains(r.MessageType))
                .OrderBy(r => r.MessageType, StringComparer.Ordinal)
                .ToList();

            // Collect message types already handled by IHandleMessages<T> interface implementations
            var interfaceMessageTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var @interface in handlerType.AllInterfaces.Where(IsHandlerInterface))
            {
                if (@interface.TypeArguments[0] is INamedTypeSymbol msgType)
                {
                    interfaceMessageTypes.Add(msgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }

            var conventionBasedMethods = ParseConventionBasedMethods(semanticModel, handlerType, markers, interfaceMessageTypes, includeInheritedMethods: !isInterfaceBased, cancellationToken);
            bool isMixed = isInterfaceBased && conventionBasedMethods.Count > 0;

            // For pure convention-based only (not interface-based, not mixed)
            var effectiveConventionBased = !isInterfaceBased
                ? conventionBasedMethods.ToImmutableEquatableArray()
                : ImmutableEquatableArray<ConventionBasedMethodSpec>.Empty;

            return new HandlerSpec(baseHandlerSpec, registrations.ToImmutableEquatableArray(), effectiveConventionBased, isMixed);
        }

        static List<ConventionBasedMethodSpec> ParseConventionBasedMethods(
            SemanticModel semanticModel,
            INamedTypeSymbol handlerType,
            MarkerTypes markers,
            HashSet<string> interfaceMessageTypes,
            bool includeInheritedMethods,
            CancellationToken cancellationToken)
        {
            var result = new List<ConventionBasedMethodSpec>();
            var handlerTypeFqn = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var iMessageHandlerContext = semanticModel.Compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");
            var cancellationTokenType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            var activatorUtilitiesConstructorAttributeType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructorAttribute");

            if (iMessageHandlerContext is null)
            {
                return result;
            }

            // Ctor params of the handler type (for instance methods)
            var selectedConstructor = SelectConstructor(handlerType, activatorUtilitiesConstructorAttributeType);
            var ctorParams = GetCtorParams(selectedConstructor, cancellationTokenType);

            foreach (var method in GetHandleMethods(handlerType, includeInheritedMethods))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (method.Name != "Handle" ||
                    method.DeclaredAccessibility != Accessibility.Public ||
                    method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
                    method.IsAbstract)
                {
                    continue;
                }

                if (method.Parameters.Length < 2)
                {
                    continue;
                }

                // Second param must be IMessageHandlerContext
                var secondParam = method.Parameters[1];
                if (!SymbolEqualityComparer.Default.Equals(secondParam.Type.OriginalDefinition, iMessageHandlerContext) &&
                    !SymbolEqualityComparer.Default.Equals(secondParam.Type, iMessageHandlerContext))
                {
                    continue;
                }

                // First param must be a named type (the message)
                if (method.Parameters[0].Type is not INamedTypeSymbol messageType)
                {
                    continue;
                }

                // Return type must be Task
                if (!HandlerConventions.IsSupportedHandlerReturnType(method.ReturnType))
                {
                    continue;
                }

                var messageTypeFqn = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (!method.IsStatic && selectedConstructor is null)
                {
                    // Convention-based instance handlers require an accessible constructor.
                    continue;
                }

                // If the class implements IHandleMessages<T> for this exact message type with exactly 2 params,
                // this Handle method is the interface implementation — not a convention-based method.
                if (method.Parameters.Length == 2 && interfaceMessageTypes.Contains(messageTypeFqn))
                {
                    continue;
                }

                // Collect extra method params (beyond message + context)
                var methodParams = new List<InjectedParamSpec>();
                for (int i = 2; i < method.Parameters.Length; i++)
                {
                    var param = method.Parameters[i];
                    bool isCt = cancellationTokenType is not null &&
                                SymbolEqualityComparer.Default.Equals(param.Type, cancellationTokenType);
                    methodParams.Add(new InjectedParamSpec(
                        param.Name,
                        param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        isCt));
                }

                var hierarchy = new ImmutableEquatableArray<string>(
                    GetTypeHierarchy(messageType, markers)
                        .Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

                var adapterName = BuildAdapterName(handlerType, method, handlerTypeFqn);

                result.Add(new ConventionBasedMethodSpec(
                    messageTypeFqn,
                    hierarchy,
                    method.IsStatic,
                    handlerTypeFqn,
                    method.IsStatic ? ImmutableEquatableArray<InjectedParamSpec>.Empty : ctorParams,
                    methodParams.ToImmutableEquatableArray(),
                    adapterName));
            }

            return result;
        }

        static IEnumerable<IMethodSymbol> GetHandleMethods(INamedTypeSymbol handlerType, bool includeInheritedMethods)
        {
            var seenSignatures = new HashSet<MethodSignatureKey>();

            for (var current = handlerType; current is not null; current = includeInheritedMethods ? current.BaseType : null)
            {
                foreach (var method in current.GetMembers("Handle").OfType<IMethodSymbol>())
                {
                    var signature = GetMethodSignature(method);
                    if (seenSignatures.Add(signature))
                    {
                        yield return method;
                    }
                }
            }
        }

        static MethodSignatureKey GetMethodSignature(IMethodSymbol method) =>
            new(
                method.IsStatic,
                method.Parameters.Select(static parameter =>
                    new ParameterSignatureKey(
                        parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        parameter.RefKind)).ToImmutableEquatableArray());

        readonly record struct MethodSignatureKey(
            bool IsStatic,
            ImmutableEquatableArray<ParameterSignatureKey> Parameters);

        readonly record struct ParameterSignatureKey(
            string ParameterType,
            RefKind RefKind);

        static ImmutableEquatableArray<InjectedParamSpec> GetCtorParams(
            IMethodSymbol? constructor,
            INamedTypeSymbol? cancellationTokenType)
        {
            if (constructor is null || constructor.Parameters.Length == 0)
            {
                return ImmutableEquatableArray<InjectedParamSpec>.Empty;
            }

            var specs = new InjectedParamSpec[constructor.Parameters.Length];
            for (int i = 0; i < constructor.Parameters.Length; i++)
            {
                var p = constructor.Parameters[i];
                bool isCt = cancellationTokenType is not null &&
                            SymbolEqualityComparer.Default.Equals(p.Type, cancellationTokenType);
                specs[i] = new InjectedParamSpec(p.Name, p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), isCt);
            }

            return specs.ToImmutableEquatableArray();
        }

        static IMethodSymbol? SelectConstructor(INamedTypeSymbol handlerType, INamedTypeSymbol? activatorUtilitiesConstructorAttributeType)
        {
            var candidates = handlerType.Constructors
                .Where(static candidate => !candidate.IsStatic && candidate.DeclaredAccessibility != Accessibility.Private)
                .ToArray();

            if (candidates.Length == 0)
            {
                return null;
            }

            if (activatorUtilitiesConstructorAttributeType is not null)
            {
                var markedConstructors = candidates
                    .Where(candidate => HasAttribute(candidate, activatorUtilitiesConstructorAttributeType))
                    .ToArray();

                if (markedConstructors.Length > 0)
                {
                    return PickMostGreedy(markedConstructors);
                }
            }

            return PickMostGreedy(candidates);
        }

        static bool HasAttribute(IMethodSymbol constructor, INamedTypeSymbol attributeType)
        {
            foreach (var attribute in constructor.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass is null)
                {
                    continue;
                }

                if (SymbolEqualityComparer.Default.Equals(attributeClass, attributeType) ||
                    SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, attributeType))
                {
                    return true;
                }
            }

            return false;
        }

        static IMethodSymbol PickMostGreedy(IEnumerable<IMethodSymbol> candidates)
        {
            IMethodSymbol? best = null;
            var bestLocation = int.MaxValue;

            foreach (var candidate in candidates)
            {
                if (best is null || candidate.Parameters.Length > best.Parameters.Length)
                {
                    best = candidate;
                    bestLocation = GetDeclarationLocation(candidate);
                    continue;
                }

                if (candidate.Parameters.Length == best.Parameters.Length)
                {
                    var candidateLocation = GetDeclarationLocation(candidate);
                    if (candidateLocation < bestLocation)
                    {
                        best = candidate;
                        bestLocation = candidateLocation;
                    }
                }
            }

            return best!;
        }

        static int GetDeclarationLocation(IMethodSymbol method)
        {
            foreach (var location in method.Locations)
            {
                if (location.IsInSource)
                {
                    return location.SourceSpan.Start;
                }
            }

            return int.MaxValue;
        }

        static string BuildAdapterName(INamedTypeSymbol handlerType, IMethodSymbol method, string handlerTypeFqn)
        {
            // Hash input: declaring type FQN + method name + static/instance + ordered params (type FQN:refkind) + return type FQN
            var sb = new StringBuilder();
            sb.Append(handlerTypeFqn);
            sb.Append('|');
            sb.Append(method.Name);
            sb.Append('|');
            sb.Append(method.IsStatic ? "static" : "instance");
            foreach (var p in method.Parameters)
            {
                sb.Append('|');
                sb.Append(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                sb.Append(':');
                sb.Append((int)p.RefKind);
                sb.Append(':');
                sb.Append((int)p.NullableAnnotation);
            }
            sb.Append('|');
            sb.Append(method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            var hash = NonCryptographicHash.GetHash(sb.ToString());

            // Simple name parts: declaring type class name chain + message type class name
            var displayParts = handlerType.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat);
            var typeName = string.Join("__", displayParts.Where(x => x.Kind == Microsoft.CodeAnalysis.SymbolDisplayPartKind.ClassName));
            var messageParts = method.Parameters[0].Type.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat);
            var messageName = string.Join("__", messageParts.Where(x => x.Kind == Microsoft.CodeAnalysis.SymbolDisplayPartKind.ClassName));

            return $"{typeName}__Handle__{messageName}_{hash:x16}";
        }

        static bool IsHandlerInterface(INamedTypeSymbol type) => HandlerConventions.IsHandlerInterface(type);

        static IEnumerable<INamedTypeSymbol> GetTypeHierarchy(INamedTypeSymbol type, MarkerTypes markers) =>
            // This matches the behavior of the reflection-based code, but it's unclear why this ordering is needed.
            // It would be more efficient to yield the base types (except where type.SpecialType is not SpecialType.System_Object)
            // and then to yield the interfaces from type.AllInterfaces except those in the MarkerTypes.
            // We're hesitant to change the implementation, however, due to wire compatibility concerns of outputting
            // an EnclosedMessageTypes header with a different ordering.
            GetParentTypes(type)
                .Where(t => !markers.IsMarkerInterface(t))
                .Select(t => new { Type = t, Rank = PlaceInMessageHierarchy(t) })
                .OrderByDescending(item => item.Rank)
                .Select(item => item.Type);

        static IEnumerable<INamedTypeSymbol> GetParentTypes(INamedTypeSymbol type)
        {
            // All interfaces implemented by the type (includes inherited interfaces)
            foreach (var iface in type.AllInterfaces)
            {
                yield return iface;
            }

            // All base types up to but excluding System.Object
            var currentBase = type.BaseType;
            while (currentBase is { SpecialType: not SpecialType.System_Object })
            {
                if (currentBase is { } named)
                {
                    yield return named;
                }

                currentBase = currentBase.BaseType;
            }
        }

        static int PlaceInMessageHierarchy(INamedTypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Interface)
            {
                // Approximate: number of interfaces implemented by this interface
                return type.AllInterfaces.Length;
            }

            var result = 0;
            var current = type.BaseType;
            while (current is not null)
            {
                result++;
                current = current.BaseType;
            }

            return result;
        }

        class MarkerTypes(Compilation compilation)
        {
            readonly INamedTypeSymbol IMessage = compilation.GetTypeByMetadataName("NServiceBus.IMessage")!;
            readonly INamedTypeSymbol ICommand = compilation.GetTypeByMetadataName("NServiceBus.ICommand")!;
            readonly INamedTypeSymbol IEvent = compilation.GetTypeByMetadataName("NServiceBus.IEvent")!;

            public bool IsMarkerInterface(INamedTypeSymbol type) =>
                SymbolEqualityComparer.Default.Equals(type, IMessage) ||
                SymbolEqualityComparer.Default.Equals(type, ICommand) ||
                SymbolEqualityComparer.Default.Equals(type, IEvent);
        }
    }
}