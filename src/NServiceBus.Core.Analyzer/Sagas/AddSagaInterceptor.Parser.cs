#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Utility;
using static Sagas;

public sealed partial class AddSagaInterceptor
{
    internal readonly record struct InterceptableSagaSpec(InterceptLocationSpec LocationSpec, SagaSpec SagaSpec);

    internal readonly record struct InterceptableSagaSpecs(ImmutableEquatableArray<InterceptableSagaSpec> Sagas);

    internal static class Parser
    {
        public static bool SyntaxLooksLikeAddSagaMethod(SyntaxNode node) => node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax
                {
                    Identifier.ValueText: AddSagaMethodName,
                    TypeArgumentList.Arguments.Count: 1
                }
            },
            ArgumentList.Arguments.Count: 0
        };

        static bool IsAddSagaMethod(IMethodSymbol method) => method is
        {
            Name: AddSagaMethodName,
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

        public static InterceptableSagaSpec? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken = default)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            var semanticModel = ctx.SemanticModel;
            if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
            {
                return null;
            }

            // Make sure the method we're looking at is ours and not some (extremely unlikely) copycat
            if (!IsAddSagaMethod(operation.TargetMethod))
            {
                return null;
            }

            if (operation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol sagaType)
            {
                return null;
            }

            if (semanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
            {
                return null;
            }

            var sagaSpec = Sagas.Parser.Parse(semanticModel, sagaType, cancellationToken: cancellationToken);
            if (sagaSpec is null)
            {
                return null;
            }

            return new InterceptableSagaSpec(InterceptLocationSpec.From(location), sagaSpec);
        }

        const string AddSagaMethodName = "AddSaga";
        const string AddHandlerClassName = "SagaRegistrationExtensions";
    }
}