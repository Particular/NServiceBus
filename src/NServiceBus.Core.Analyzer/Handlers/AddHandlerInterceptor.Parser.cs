#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Utility;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;
using static Handlers;

public sealed partial class AddHandlerInterceptor
{
    internal readonly record struct InterceptableHandlerSpec(InterceptLocationSpec LocationSpec, HandlerSpec HandlerSpec);

    internal readonly record struct InterceptableHandlerSpecs(ImmutableEquatableArray<InterceptableHandlerSpec> Handlers);

    internal static class Parser
    {
        public static bool SyntaxLooksLikeAddHandlerMethod(SyntaxNode node) => HandlerSyntaxConventions.SyntaxLooksLikeAddHandlerMethod(node);

        internal static bool IsAddHandlerMethod(IMethodSymbol method) => HandlerSyntaxConventions.IsAddHandlerMethod(method);

        public static InterceptableHandlerSpec? Parse(InvocationExpressionSyntax invocation, SemanticModel semanticModel, HandlerKnownTypes knownTypes, CancellationToken cancellationToken = default)
        {
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

            var handlerSpec = Handlers.Parser.Parse(handlerType, BaseParser.SpecKind.Handler, knownTypes, cancellationToken);
            return new InterceptableHandlerSpec(InterceptLocationSpec.From(location), handlerSpec);
        }
    }
}