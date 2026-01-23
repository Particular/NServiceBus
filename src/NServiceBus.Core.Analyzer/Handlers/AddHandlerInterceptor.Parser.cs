#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Utility;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;

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

        public static InterceptableHandlerSpec? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken = default)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            var semanticModel = ctx.SemanticModel;
            if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
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

            if (semanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
            {
                return null;
            }

            var handlerSpec = Handlers.Parser.Parse(semanticModel, handlerType, BaseParser.SpecKind.Handler, cancellationToken: cancellationToken);
            return new InterceptableHandlerSpec(InterceptLocationSpec.From(location), handlerSpec);
        }

        const string AddHandlerMethodName = "AddHandler";
        const string AddHandlerClassName = "MessageHandlerRegistrationExtensions";
    }
}