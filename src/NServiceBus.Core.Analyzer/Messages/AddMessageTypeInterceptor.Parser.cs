#nullable enable

namespace NServiceBus.Core.Analyzer.Messages;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
        public static MessageTypeSpec From(INamedTypeSymbol messageType) =>
            new(
                string.Join("__", messageType.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat).Where(x => x.Kind == SymbolDisplayPartKind.ClassName)),
                messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                GetHierarchyTypeNames(messageType));

        static ImmutableEquatableArray<string> GetHierarchyTypeNames(INamedTypeSymbol messageType)
        {
            // Mirrors the runtime hierarchy inference used by MessageMetadataRegistry.GetRuntimeMessageHierarchy:
            // interfaces ordered by their own interface count descending, followed by base classes ordered from
            // deepest to shallowest. Non-message parents are filtered by the convention when the registry is
            // initialized, so including all interfaces and base types is safe.
            var interfaces = messageType.AllInterfaces
                .OrderByDescending(i => i.AllInterfaces.Length)
                .ThenBy(i => i.ToDisplayString(), StringComparer.Ordinal)
                .Select(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            var baseTypes = new List<string>();
            var currentBaseType = messageType.BaseType;
            while (currentBaseType is not null && currentBaseType.SpecialType != SpecialType.System_Object)
            {
                baseTypes.Add(currentBaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                currentBaseType = currentBaseType.BaseType;
            }

            return interfaces.Concat(baseTypes).ToImmutableEquatableArray();
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

            return new InterceptableMessageTypeSpec(InterceptLocationSpec.From(location), MessageTypeSpec.From(messageType));
        }

        const string AddMessageTypeMethodName = "AddMessageType";
        const string AddMessageTypeClassName = "MessageTypeRegistrationExtensions";
    }
}
