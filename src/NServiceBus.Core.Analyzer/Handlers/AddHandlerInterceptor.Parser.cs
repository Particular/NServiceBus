#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

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
    static class Parser
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
            Name: "IHandleMessages" or "IHandleTimeouts",
            IsGenericType: true,
            ContainingNamespace:
            {
                Name: "NServiceBus",
                ContainingNamespace.IsGlobalNamespace: true
            }
        };

        public static HandlerSpec? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken = default)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            if (ctx.SemanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
            {
                return null;
            }

            // Make sure the method we're looking at is ours and not some (extremely unlikely) copycat
            if (!IsAddHandlerMethod(operation.TargetMethod))
            {
                return null;
            }

            if (operation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol handlerType)
            {
                return null;
            }

            var registrations = handlerType.AllInterfaces
                .Where(IsHandlerInterface)
                .Select(type =>
                {
                    var messageType = type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var addType = type.Name == "IHandleTimeouts" ? "Timeout" : "Message";
                    return new RegistrationSpec(addType, messageType);
                })
                .ToImmutableArray();

            if (ctx.SemanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
            {
                return null;
            }

            var handlerFullyQualifiedName = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return new HandlerSpec(InterceptLocationSpec.From(location), handlerType.Name, handlerFullyQualifiedName, registrations);
        }

        const string AddHandlerMethodName = "AddHandler";
        const string AddHandlerClassName = "MessageHandlerRegistrationExtensions";
    }
}