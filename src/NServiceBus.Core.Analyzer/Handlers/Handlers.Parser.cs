#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;

static partial class Handlers
{
    public sealed record HandlerSpec : BaseParser.BaseSpec
    {
        public HandlerSpec(BaseParser.BaseSpec handlerBaseSpec,
            ImmutableEquatableArray<RegistrationSpec> registrations) : base(handlerBaseSpec) =>
            Registrations = registrations;

        public ImmutableEquatableArray<RegistrationSpec> Registrations { get; }
    }

    public readonly record struct HandlerSpecs(ImmutableEquatableArray<HandlerSpec> Handlers);

    public enum RegistrationType
    {
        MessageHandler,
        StartMessageHandler,
        TimeoutHandler,
    }

    public readonly record struct RegistrationSpec(RegistrationType RegistrationType, string MessageType, ImmutableEquatableArray<string> MessageHierarchy, string HandlerType);

    public static class Parser
    {
        public static HandlerSpec Parse(SemanticModel semanticModel, INamedTypeSymbol handlerType, CancellationToken cancellationToken = default)
        {
            var handlerSpec = BaseParser.Parse(handlerType);

            var allRegistrations = new List<RegistrationSpec>();
            var startedMessageTypes = new HashSet<string>();
            var markers = new MarkerTypes(semanticModel.Compilation);

            foreach (var @interface in handlerType.AllInterfaces.Where(IsHandlerInterface))
            {
                cancellationToken.ThrowIfCancellationRequested();

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
                var spec = new RegistrationSpec(registrationType.Value, messageTypeName, hierarchy, handlerSpec.FullyQualifiedName);
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

            return new HandlerSpec(handlerSpec, registrations.ToImmutableEquatableArray());
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