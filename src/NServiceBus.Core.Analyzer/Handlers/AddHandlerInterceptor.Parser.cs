#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Utility;

public sealed partial class AddHandlerInterceptor
{
    internal static class Parser
    {
        public static bool SyntaxLooksLikeAddHandlerMethod(SyntaxNode node) => node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax
                {
                    Identifier.ValueText: AddHandlerMethodName,
                    TypeArgumentList.Arguments.Count: 1
                }
            },
            ArgumentList.Arguments.Count: 0
        };

        static bool IsAddHandlerMethod(IMethodSymbol method) => method is
        {
            Name: AddHandlerMethodName,
            IsGenericMethod: true,
            TypeArguments.Length: 1,
            ContainingType:
            {
                Name: AddHandlerClassName,
                ContainingNamespace:
                {
                    Name: "NServiceBus",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };

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

        public static ImmutableArray<HandlerSpec> Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken = default)
        {
            var builder = ImmutableArray.CreateBuilder<HandlerSpec>();
            var iMessage = ctx.SemanticModel.Compilation.GetTypeByMetadataName("NServiceBus.IMessage");
            var iCommand = ctx.SemanticModel.Compilation.GetTypeByMetadataName("NServiceBus.ICommand");
            var iEvent = ctx.SemanticModel.Compilation.GetTypeByMetadataName("NServiceBus.IEvent");

            foreach (var invocation in ctx.TargetNode.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!SyntaxLooksLikeAddHandlerMethod(invocation))
                {
                    continue;
                }

                if (ctx.SemanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
                {
                    continue;
                }

                // Make sure the method we're looking at is ours and not some (extremely unlikely) copycat
                if (!IsAddHandlerMethod(operation.TargetMethod))
                {
                    continue;
                }

                var spec = Parse(ctx.SemanticModel, operation, invocation, cancellationToken);
                if (spec is not null)
                {
                    builder.Add(spec);
                }
            }

            return builder.ToImmutable();
        }

        public static HandlerSpec? Parse(SemanticModel semanticModel, IInvocationOperation operation, InvocationExpressionSyntax invocation, CancellationToken cancellationToken = default)
        {
            if (operation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol handlerType)
            {
                return null;
            }

            var allRegistrations = new List<RegistrationSpec>();
            var startedMessageTypes = new HashSet<string>();
            var markers = new MarkerTypes(semanticModel.Compilation);

            foreach (var iface in handlerType.AllInterfaces.Where(IsHandlerInterface))
            {
                if (iface.TypeArguments[0] is not INamedTypeSymbol messageType)
                {
                    continue;
                }

                var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                RegistrationType? registrationType = iface.Name switch
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
                var spec = new RegistrationSpec(registrationType.Value, messageTypeName, hierarchy);
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
                .ToImmutableEquatableArray();

            if (semanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
            {
                return null;
            }

            var handlerFullyQualifiedName = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return new HandlerSpec(InterceptLocationSpec.From(location), handlerType.Name, handlerFullyQualifiedName, registrations);
        }

        static IEnumerable<INamedTypeSymbol> GetTypeHierarchy(INamedTypeSymbol type, MarkerTypes markers)
        {
            var current = type.BaseType;
            while (current is { SpecialType: not SpecialType.System_Object })
            {
                yield return current;
                current = current.BaseType;
            }

            foreach (var iface in type.AllInterfaces)
            {
                if (!markers.IsMarkerInterface(iface))
                {
                    yield return iface;
                }
            }
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

        const string AddHandlerMethodName = "AddHandler";
        const string AddHandlerClassName = "MessageHandlerRegistrationExtensions";
    }
}