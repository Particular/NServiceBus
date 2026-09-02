#nullable enable

namespace NServiceBus.Core.Analyzer.Messages;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Handlers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using NServiceBus.Core.Analyzer;
using Utility;

public sealed partial class AddMessageTypeInterceptor
{
    internal readonly record struct MessageTypeSpec(string Name, string FullyQualifiedName, ImmutableEquatableArray<string> HierarchyTypeNames)
    {
        public static MessageTypeSpec From(INamedTypeSymbol messageType, Compilation compilation) =>
            new(
                string.Join("__", messageType.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat).Where(x => x.Kind == SymbolDisplayPartKind.ClassName)),
                messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                GetHierarchyTypeNames(messageType, compilation));

        static ImmutableEquatableArray<string> GetHierarchyTypeNames(INamedTypeSymbol messageType, Compilation compilation)
        {
            // Shared with handler/saga generation so the emitted hierarchy has the same ordering regardless of which
            // registration path wins (duplicate registration is first-wins in the message metadata registry).
            return MessageHierarchyBuilder.GetTypeHierarchy(messageType, new MarkerTypes(compilation))
                .Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .ToImmutableEquatableArray();
        }
    }

    internal readonly record struct InterceptableMessageTypeSpec(InterceptLocationSpec LocationSpec, MessageTypeSpec MessageTypeSpec);

    internal readonly record struct InterceptableMessageTypeSpecs(ImmutableEquatableArray<InterceptableMessageTypeSpec> MessageTypes);

    internal static class Parser
    {
        public static bool SyntaxLooksLikeAddMessageTypeMethod(SyntaxNode node) => node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax
                {
                    Identifier.ValueText: AddMessageTypeMethodName,
                    TypeArgumentList.Arguments.Count: 1
                }
            },
            ArgumentList.Arguments.Count: 0
        };

        internal static bool IsAddMessageTypeMethod(IMethodSymbol method) => method is
        {
            Name: AddMessageTypeMethodName,
            IsGenericMethod: true,
            TypeArguments.Length: 1,
            ContainingType:
            {
                Name: AddMessageTypeClassName,
                ContainingNamespace:
                {
                    Name: "NServiceBus",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };

        public static InterceptableMessageTypeSpec? Parse(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken = default)
        {
            if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
            {
                return null;
            }

            // Make sure the method we're looking at is ours and not some (extremely unlikely) copycat
            if (!IsAddMessageTypeMethod(operation.TargetMethod))
            {
                return null;
            }

            if (operation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol messageType)
            {
                return null;
            }

            if (semanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
            {
                return null;
            }

            return new InterceptableMessageTypeSpec(InterceptLocationSpec.From(location), MessageTypeSpec.From(messageType, semanticModel.Compilation));
        }

        const string AddMessageTypeMethodName = "AddMessageType";
        const string AddMessageTypeClassName = "MessageTypeRegistrationExtensions";
    }
}
