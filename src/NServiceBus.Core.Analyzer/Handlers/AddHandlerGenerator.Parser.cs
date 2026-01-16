#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Utility;

public sealed partial class AddHandlerGenerator
{
    internal static class Parser
    {
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

        public static HandlerSpec? Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken = default)
        {
            if (ctx.TargetSymbol is INamedTypeSymbol namedTypeSymbol && Parse(ctx.SemanticModel, namedTypeSymbol, cancellationToken) is { } spec)
            {
                return spec;
            }
            return null;
        }

        static HandlerSpec Parse(SemanticModel semanticModel, INamedTypeSymbol handlerType, CancellationToken cancellationToken)
        {
            var handlerFullyQualifiedName = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var handlerNamespace = GetHandlerNamespace(handlerType);
            var assemblyName = handlerType.ContainingAssembly?.Name ?? "Assembly";
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
                var spec = new RegistrationSpec(registrationType.Value, messageTypeName, hierarchy, handlerFullyQualifiedName);
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

            var interfaceLessHandlers = ParseInterfaceLessHandlers(semanticModel, handlerType, handlerFullyQualifiedName, markers, registrations, cancellationToken);

            if (interfaceLessHandlers.Count > 0)
            {
                var existingMessageRegistrations = new HashSet<string>(registrations.Where(reg =>
                        reg.RegistrationType is RegistrationType.MessageHandler or RegistrationType.StartMessageHandler)
                    .Select(reg => reg.MessageType), StringComparer.Ordinal);

                foreach (var handler in interfaceLessHandlers)
                {
                    if (existingMessageRegistrations.Contains(handler.MessageType))
                    {
                        continue;
                    }

                    registrations.Add(new RegistrationSpec(RegistrationType.MessageHandler, handler.MessageType, handler.MessageHierarchy, handler.GeneratedHandlerType));
                }

                registrations = registrations
                    .OrderBy(r => r.MessageType, StringComparer.Ordinal)
                    .ToList();
            }

            return new HandlerSpec(handlerType.Name, handlerFullyQualifiedName, handlerNamespace, assemblyName, registrations.ToImmutableEquatableArray(), interfaceLessHandlers);
        }

        static ImmutableEquatableArray<InterfaceLessHandlerSpec> ParseInterfaceLessHandlers(
            SemanticModel semanticModel,
            INamedTypeSymbol handlerType,
            string handlerFullyQualifiedName,
            MarkerTypes markers,
            IReadOnlyCollection<RegistrationSpec> registrations,
            CancellationToken cancellationToken)
        {
            var taskType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var contextType = semanticModel.Compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");

            if (taskType is null || contextType is null)
            {
                return ImmutableEquatableArray<InterfaceLessHandlerSpec>.Empty;
            }

            var registeredMessages = new HashSet<string>(registrations.Where(reg =>
                    reg.RegistrationType is RegistrationType.MessageHandler or RegistrationType.StartMessageHandler)
                .Select(reg => reg.MessageType), StringComparer.Ordinal);

            var interfaceLessHandlers = new List<InterfaceLessHandlerSpec>();

            foreach (var method in handlerType.GetMembers().OfType<IMethodSymbol>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsInterfaceLessHandleMethod(method, taskType, contextType))
                {
                    continue;
                }

                if (method.Parameters[0].Type is not INamedTypeSymbol messageType || messageType.TypeKind == TypeKind.Error)
                {
                    continue;
                }

                var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (registeredMessages.Contains(messageTypeName))
                {
                    continue;
                }

                var hierarchy = new ImmutableEquatableArray<string>(GetTypeHierarchy(messageType, markers).Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                var constructorParameters = method.Parameters
                    .Skip(2)
                    .Select(CreateParameterSpec)
                    .ToImmutableEquatableArray();

                var parameterSignature = BuildParameterSignature(method);
                var hash = NonCryptographicHash.GetHash(handlerFullyQualifiedName, parameterSignature);
                var generatedHandlerName = $"{handlerType.Name}_Handle_{hash:x16}";
                var generatedHandlerType = $"global::{generatedHandlerName}";

                interfaceLessHandlers.Add(new InterfaceLessHandlerSpec(method.IsStatic,
                    generatedHandlerName,
                    generatedHandlerType,
                    handlerFullyQualifiedName,
                    messageTypeName,
                    hierarchy,
                    constructorParameters));
            }

            return interfaceLessHandlers
                .OrderBy(spec => spec.GeneratedHandlerType, StringComparer.Ordinal)
                .ToImmutableEquatableArray();
        }

        static string BuildParameterSignature(IMethodSymbol method) =>
            string.Join(", ", method.Parameters.Select(p => p.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        static bool IsInterfaceLessHandleMethod(IMethodSymbol method, INamedTypeSymbol taskType, INamedTypeSymbol contextType) =>
            method is
            {
                Name: "Handle",
                DeclaredAccessibility: Accessibility.Public,
                Parameters.Length: >= 2,
            } &&
            SymbolEqualityComparer.Default.Equals(method.ReturnType, taskType) &&
            SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, contextType);

        static ParameterSpec CreateParameterSpec(IParameterSymbol parameter)
        {
            var type = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var parameterName = EscapeIdentifier(parameter.Name);
            var fieldName = EscapeIdentifier($"_{parameter.Name}");
            return new ParameterSpec(type, parameterName, fieldName);
        }

        static string EscapeIdentifier(string identifier)
        {
            if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
                SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None)
            {
                return $"@{identifier}";
            }

            return identifier;
        }

        static string GetHandlerNamespace(INamedTypeSymbol handlerType)
        {
            var handlerNamespace = handlerType.ContainingNamespace;
            if (handlerNamespace is null || handlerNamespace.IsGlobalNamespace)
            {
                return string.Empty;
            }

            var parts = new Stack<string>();

            while (handlerNamespace is { IsGlobalNamespace: false })
            {
                parts.Push(handlerNamespace.Name);
                handlerNamespace = handlerNamespace.ContainingNamespace;
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(".", parts);
        }

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