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
            ImmutableEquatableArray<InterfaceLessMethodSpec> interfaceLessMethods,
            bool isMixed) : base(handlerBaseSpec)
        {
            Registrations = registrations;
            InterfaceLessMethods = interfaceLessMethods;
            IsMixed = isMixed;
        }

        public ImmutableEquatableArray<RegistrationSpec> Registrations { get; }
        public ImmutableEquatableArray<InterfaceLessMethodSpec> InterfaceLessMethods { get; }
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

    public readonly record struct InterfaceLessMethodSpec(
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

            var interfaceLessMethods = ParseInterfaceLessMethods(semanticModel, handlerType, markers, interfaceMessageTypes, cancellationToken);
            bool isMixed = isInterfaceBased && interfaceLessMethods.Count > 0;

            // For pure interface-less only (not interface-based, not mixed)
            var effectiveInterfaceLess = !isInterfaceBased
                ? interfaceLessMethods.ToImmutableEquatableArray()
                : ImmutableEquatableArray<InterfaceLessMethodSpec>.Empty;

            return new HandlerSpec(baseHandlerSpec, registrations.ToImmutableEquatableArray(), effectiveInterfaceLess, isMixed);
        }

        static List<InterfaceLessMethodSpec> ParseInterfaceLessMethods(
            SemanticModel semanticModel,
            INamedTypeSymbol handlerType,
            MarkerTypes markers,
            HashSet<string> interfaceMessageTypes,
            CancellationToken cancellationToken)
        {
            var result = new List<InterfaceLessMethodSpec>();
            var handlerTypeFqn = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var iMessageHandlerContext = semanticModel.Compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");
            var cancellationTokenType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

            if (iMessageHandlerContext is null)
            {
                return result;
            }

            // Ctor params of the handler type (for instance methods)
            var ctorParams = GetCtorParams(handlerType, cancellationTokenType);

            foreach (var member in handlerType.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (member is not IMethodSymbol method)
                {
                    continue;
                }

                if (method.Name != "Handle" || method.DeclaredAccessibility != Accessibility.Public)
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

                // Return type must be Task-like
                if (!IsTaskLike(method.ReturnType))
                {
                    continue;
                }

                var messageTypeFqn = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // If the class implements IHandleMessages<T> for this exact message type with exactly 2 params,
                // this Handle method is the interface implementation — not an interface-less method.
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

                result.Add(new InterfaceLessMethodSpec(
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

        static ImmutableEquatableArray<InjectedParamSpec> GetCtorParams(INamedTypeSymbol handlerType, INamedTypeSymbol? cancellationTokenType)
        {
            // Pick the constructor with the most parameters (primary or longest)
            IMethodSymbol? ctor = null;
            foreach (var candidate in handlerType.Constructors)
            {
                if (candidate.IsStatic || candidate.DeclaredAccessibility == Accessibility.Private)
                {
                    continue;
                }

                if (ctor is null || candidate.Parameters.Length > ctor.Parameters.Length)
                {
                    ctor = candidate;
                }
            }

            if (ctor is null || ctor.Parameters.Length == 0)
            {
                return ImmutableEquatableArray<InjectedParamSpec>.Empty;
            }

            var specs = new InjectedParamSpec[ctor.Parameters.Length];
            for (int i = 0; i < ctor.Parameters.Length; i++)
            {
                var p = ctor.Parameters[i];
                bool isCt = cancellationTokenType is not null &&
                            SymbolEqualityComparer.Default.Equals(p.Type, cancellationTokenType);
                specs[i] = new InjectedParamSpec(p.Name, p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), isCt);
            }

            return specs.ToImmutableEquatableArray();
        }

        static string BuildAdapterName(
            INamedTypeSymbol handlerType,
            IMethodSymbol method,
            string handlerTypeFqn)
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

        static bool IsTaskLike(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_Void)
            {
                return false;
            }

            var name = type.Name;
            return name is "Task" or "ValueTask";
        }

        static bool IsHandlerInterface(INamedTypeSymbol type) => type is
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