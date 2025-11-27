#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Handlers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Utility;

public sealed partial class AddSagaInterceptor
{
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

        public static SagaSpec? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken = default)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            if (ctx.SemanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
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

            // Extract saga data type from Saga<TSagaData>
            var sagaDataType = GetSagaDataType(sagaType);
            if (sagaDataType == null)
            {
                return null;
            }

            // Get interceptable location for code generation
            if (ctx.SemanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
            {
                return null;
            }

            var handlerSpec = AddHandlerInterceptor.Parser.Parse(ctx, operation, invocation, cancellationToken);
            if (handlerSpec == null)
            {
                return null;
            }

            var methodName = CreateMethodName(sagaType);
            var sagaFullyQualifiedName = sagaType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var sagaDataFullyQualifiedName = sagaDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Analyze ConfigureHowToFindSaga to extract mappings
            var propertyMappings = ExtractPropertyMappings(sagaType, ctx.SemanticModel, cancellationToken);

            return new SagaSpec(
                InterceptLocationSpec.From(location),
                methodName,
                sagaFullyQualifiedName,
                sagaDataFullyQualifiedName,
                propertyMappings,
                handlerSpec);
        }

        static INamedTypeSymbol? GetSagaDataType(INamedTypeSymbol sagaType)
        {
            // Find Saga<TSagaData> in the inheritance chain
            var baseType = sagaType.BaseType;
            while (baseType != null)
            {
                if (baseType is { IsGenericType: true, Name: "Saga", TypeArguments.Length: 1 })
                {
                    if (baseType.TypeArguments[0] is INamedTypeSymbol sagaDataType)
                    {
                        return sagaDataType;
                    }
                }
                baseType = baseType.BaseType;
            }
            return null;
        }

        static string CreateMethodName(INamedTypeSymbol sagaType)
        {
            const string NamePrefix = "AddSaga_";

            var sb = new StringBuilder(NamePrefix, 50)
                .Append(sagaType.Name)
                .Append('_');

            var sagaFullName = sagaType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var hash = NonCryptographicHash.GetHash(sagaFullName);

            sb.Append(hash.ToString("x16"));

            return sb.ToString();
        }

        static ImmutableEquatableArray<PropertyMappingSpec> ExtractPropertyMappings(
            INamedTypeSymbol sagaType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var configureMethod = FindConfigureHowToFindSagaMethod(sagaType);

            if (configureMethod == null)
            {
                return ImmutableEquatableArray<PropertyMappingSpec>.Empty;
            }

            // Get syntax node from method symbol
            // Sort syntax references by file path to ensure deterministic selection
            var syntaxRefs = configureMethod.DeclaringSyntaxReferences
                .OrderBy(r => r.SyntaxTree.FilePath, StringComparer.Ordinal)
                .ThenBy(r => r.Span.Start)
                .ToArray();
            if (syntaxRefs.Length == 0)
            {
                return ImmutableEquatableArray<PropertyMappingSpec>.Empty;
            }

            var methodSyntax = syntaxRefs[0].GetSyntax(cancellationToken);
            if (methodSyntax is not MethodDeclarationSyntax methodDeclaration)
            {
                return ImmutableEquatableArray<PropertyMappingSpec>.Empty;
            }

            // Get method body (block or expression body)
            SyntaxNode? methodBody = methodDeclaration.Body ?? (SyntaxNode?)methodDeclaration.ExpressionBody?.Expression;
            if (methodBody == null)
            {
                return ImmutableEquatableArray<PropertyMappingSpec>.Empty;
            }

            var mappings = new List<PropertyMappingSpec>();
            var walker = new ConfigureMappingWalker(semanticModel, mappings, cancellationToken);
            walker.Visit(methodBody);

            // Sort mappings to ensure deterministic ordering
            return mappings.OrderBy(m => m.MessageType, StringComparer.Ordinal)
                .ToImmutableEquatableArray();
        }

        static IMethodSymbol? FindConfigureHowToFindSagaMethod(
            INamedTypeSymbol sagaType)
        {
            // Look for protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TSagaData> mapper)
            foreach (var member in sagaType.GetMembers("ConfigureHowToFindSaga"))
            {
                if (member is IMethodSymbol { IsOverride: true, DeclaredAccessibility: Accessibility.Protected, Parameters.Length: 1 } method)
                {
                    return method;
                }
            }

            return null;
        }

        static bool IsAddSagaMethod(IMethodSymbol method) => method is
        {
            Name: AddSagaMethodName,
            IsGenericMethod: true,
            TypeArguments.Length: 1,
            ContainingType:
            {
                Name: AddSagaClassName,
                ContainingNamespace:
                {
                    Name: "NServiceBus",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };

        const string AddSagaClassName = "SagaRegistrationExtensions";
        const string AddSagaMethodName = "AddSaga";

        sealed class ConfigureMappingWalker(
            SemanticModel semanticModel,
            List<PropertyMappingSpec> propertyMappings,
            CancellationToken cancellationToken)
            : CSharpSyntaxWalker
        {
            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                base.VisitInvocationExpression(node);

                // Look for .ToMessage<TMessage>(...) calls (from MapSaga syntax)
                if (node.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "ToMessage" } } toMessageAccess)
                {
                    // This is a ToMessage call from MapSaga().ToMessage<TMessage>(...)
                    // The pattern is: mapper.MapSaga(saga => saga.Prop).ToMessage<TMessage>(msg => msg.Prop)
                    AnalyzeMapSagaToMessageCall(node, toMessageAccess);
                }
            }

            void AnalyzeMapSagaToMessageCall(InvocationExpressionSyntax toMessageCall, MemberAccessExpressionSyntax toMessageAccess)
            {
                if (toMessageCall.ArgumentList.Arguments.Count <= 0)
                {
                    return;
                }

                // Extract message property and type
                var messagePropertyArg = toMessageCall.ArgumentList.Arguments[0].Expression;
                var messagePropertyName = ExtractPropertyNameFromExpression(messagePropertyArg);

                // Get message type from generic argument
                if (toMessageAccess.Name is not GenericNameSyntax { TypeArgumentList.Arguments.Count: > 0 } genericName)
                {
                    return;
                }

                var messageTypeSyntax = genericName.TypeArgumentList.Arguments[0];
                if (semanticModel.GetSymbolInfo(messageTypeSyntax, cancellationToken).Symbol is not INamedTypeSymbol messageTypeSymbol || messagePropertyName == null)
                {
                    return;
                }

                var messageTypeName = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                propertyMappings.Add(new PropertyMappingSpec(messageTypeName, messageTypeSymbol.Name, messagePropertyName));
            }

            static string? ExtractPropertyNameFromExpression(ExpressionSyntax expression)
            {
                // Handle lambda: message => message.Property
                if (expression is not LambdaExpressionSyntax lambda)
                {
                    return null;
                }

                return lambda.Body switch
                {
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                    // Handle conversion: message => (object)message.Property
                    CastExpressionSyntax { Expression: MemberAccessExpressionSyntax castMember } => castMember.Name
                        .Identifier.ValueText,
                    _ => null
                };
            }
        }
    }
}