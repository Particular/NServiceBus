#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            var propertyMappings = ExtractPropertyMappings(sagaType, sagaDataType, ctx.SemanticModel, cancellationToken);

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

        static EquatableArray<PropertyMappingSpec> ExtractPropertyMappings(
            INamedTypeSymbol sagaType,
            INamedTypeSymbol sagaDataType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var mappings = ImmutableArray.CreateBuilder<PropertyMappingSpec>();
            var configureMethod = FindConfigureHowToFindSagaMethod(sagaType);

            if (configureMethod == null)
            {
                return mappings.ToImmutable();
            }

            // Get syntax node from method symbol
            // Sort syntax references by file path to ensure deterministic selection
            var syntaxRefs = configureMethod.DeclaringSyntaxReferences
                .OrderBy(r => r.SyntaxTree.FilePath, StringComparer.Ordinal)
                .ThenBy(r => r.Span.Start)
                .ToArray();
            if (syntaxRefs.Length == 0)
            {
                return mappings.ToImmutable();
            }

            var methodSyntax = syntaxRefs[0].GetSyntax(cancellationToken);
            if (methodSyntax is not MethodDeclarationSyntax methodDeclaration)
            {
                return mappings.ToImmutable();
            }

            // Get method body (block or expression body)
            SyntaxNode? methodBody = methodDeclaration.Body ?? (SyntaxNode?)methodDeclaration.ExpressionBody?.Expression;
            if (methodBody == null)
            {
                return mappings.ToImmutable();
            }

            var walker = new ConfigureMappingWalker(semanticModel, sagaDataType, cancellationToken);
            walker.Visit(methodBody);

            // Sort mappings to ensure deterministic ordering
            foreach (var propertyMappingInfo in walker.PropertyMappings.OrderBy(m => m.MessageType, StringComparer.Ordinal))
            {
                mappings.Add(propertyMappingInfo);
            }
            return mappings.ToImmutable();
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

        class ConfigureMappingWalker(
            SemanticModel semanticModel,
            INamedTypeSymbol sagaDataType,
            CancellationToken cancellationToken)
            : CSharpSyntaxWalker
        {
            readonly List<PropertyMappingSpec> propertyMappings = [];

            public ImmutableArray<PropertyMappingSpec> PropertyMappings => [.. propertyMappings];

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                base.VisitInvocationExpression(node);

                // Look for .ToSaga(...) calls
                if (node.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.ValueText == "ToSaga")
                {
                    // This is a ToSaga call, now we need to find the ConfigureMapping or ConfigureHeaderMapping call
                    if (memberAccess.Expression is InvocationExpressionSyntax configureMappingCall)
                    {
                        AnalyzeConfigureMappingCall(configureMappingCall, node);
                    }
                }

                // Look for .ToMessage<TMessage>(...) calls (from MapSaga syntax)
                if (node.Expression is MemberAccessExpressionSyntax toMessageAccess &&
                    toMessageAccess.Name is GenericNameSyntax genericName &&
                    genericName.Identifier.ValueText == "ToMessage")
                {
                    // This is a ToMessage call from MapSaga().ToMessage<TMessage>(...)
                    // The pattern is: mapper.MapSaga(saga => saga.Prop).ToMessage<TMessage>(msg => msg.Prop)
                    // This internally calls ConfigureMapping(...).ToSaga(...), so we need to trace back
                    AnalyzeMapSagaToMessageCall(node, toMessageAccess);
                }
            }

            void AnalyzeMapSagaToMessageCall(InvocationExpressionSyntax toMessageCall, MemberAccessExpressionSyntax toMessageAccess)
            {
                // The expression structure is: [MapSaga call].ToMessage<TMessage>(...)
                // We need to find the MapSaga call and extract the saga property
                if (toMessageAccess.Expression is InvocationExpressionSyntax mapSagaCall)
                {
                    // Extract saga property from MapSaga argument
                    if (mapSagaCall.ArgumentList.Arguments.Count > 0)
                    {
                        var sagaPropertyArg = mapSagaCall.ArgumentList.Arguments[0].Expression;
                        var sagaPropertyInfo = ExtractPropertyInfoFromExpression(sagaPropertyArg, sagaDataType);

                        if (sagaPropertyInfo != null && toMessageCall.ArgumentList.Arguments.Count > 0)
                        {
                            // Extract message property and type
                            var messagePropertyArg = toMessageCall.ArgumentList.Arguments[0].Expression;
                            var messagePropertyName = ExtractPropertyNameFromExpression(messagePropertyArg);

                            // Get message type from generic argument
                            if (toMessageAccess.Name is GenericNameSyntax genericName &&
                                genericName.TypeArgumentList.Arguments.Count > 0)
                            {
                                var messageTypeSyntax = genericName.TypeArgumentList.Arguments[0];

                                if (semanticModel.GetSymbolInfo(messageTypeSyntax, cancellationToken).Symbol is INamedTypeSymbol messageTypeSymbol && messagePropertyName != null)
                                {
                                    var messageTypeName = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                    propertyMappings.Add(new PropertyMappingSpec(
                                        messageTypeName,
                                        messagePropertyName));
                                }
                            }
                        }
                    }
                }
            }

            void AnalyzeConfigureMappingCall(InvocationExpressionSyntax configureMappingCall, InvocationExpressionSyntax toSagaCall)
            {
                if (semanticModel.GetOperation(configureMappingCall, cancellationToken) is not IInvocationOperation configureOp)
                {
                    return;
                }

                var method = configureOp.TargetMethod;
                var methodName = method.Name;

                // Check if this is ConfigureMapping<TMessage>(...)
                if (methodName == "ConfigureMapping" && method.IsGenericMethod && method.TypeArguments.Length == 1)
                {
                    var messageType = method.TypeArguments[0];
                    if (messageType is INamedTypeSymbol messageTypeSymbol)
                    {
                        // Extract message property from ConfigureMapping argument
                        if (configureMappingCall.ArgumentList.Arguments.Count > 0)
                        {
                            var messagePropertyArg = configureMappingCall.ArgumentList.Arguments[0].Expression;
                            var messagePropertyName = ExtractPropertyNameFromExpression(messagePropertyArg);

                            // Extract saga property from ToSaga argument
                            if (toSagaCall.ArgumentList.Arguments.Count > 0)
                            {
                                var sagaPropertyArg = toSagaCall.ArgumentList.Arguments[0].Expression;
                                var sagaPropertyInfo = ExtractPropertyInfoFromExpression(sagaPropertyArg, sagaDataType);

                                if (messagePropertyName != null && sagaPropertyInfo != null)
                                {
                                    var messageTypeName = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                    propertyMappings.Add(new PropertyMappingSpec(
                                        messageTypeName,
                                        messagePropertyName));
                                }
                            }
                        }
                    }
                }
            }

            static string? ExtractPropertyNameFromExpression(ExpressionSyntax expression)
            {
                // Handle lambda: message => message.Property
                if (expression is LambdaExpressionSyntax lambda)
                {
                    if (lambda.Body is MemberAccessExpressionSyntax memberAccess)
                    {
                        return memberAccess.Name.Identifier.ValueText;
                    }
                    // Handle conversion: message => (object)message.Property
                    if (lambda.Body is CastExpressionSyntax cast && cast.Expression is MemberAccessExpressionSyntax castMember)
                    {
                        return castMember.Name.Identifier.ValueText;
                    }
                }

                return null;
            }

            IPropertySymbol? ExtractPropertyInfoFromExpression(ExpressionSyntax expression, INamedTypeSymbol sagaDataType)
            {
                // Handle lambda: saga => saga.Property
                if (expression is LambdaExpressionSyntax lambda)
                {
                    var body = lambda.Body;

                    // Handle conversion: saga => (object)saga.Property
                    if (body is CastExpressionSyntax cast)
                    {
                        body = cast.Expression;
                    }

                    if (body is MemberAccessExpressionSyntax memberAccess)
                    {
                        if (semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol is IPropertySymbol property)
                        {
                            // Verify the property belongs to the saga data type
                            if (property.ContainingType.Equals(sagaDataType, SymbolEqualityComparer.Default))
                            {
                                return property;
                            }
                        }
                    }
                }

                return null;
            }
        }
    }
}