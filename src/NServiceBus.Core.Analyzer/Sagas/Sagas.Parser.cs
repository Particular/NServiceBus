#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static NServiceBus.Core.Analyzer.Handlers.Handlers;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;

public static partial class Sagas
{
    public readonly record struct SagaSpecs(ImmutableEquatableArray<SagaSpec> Sagas);

    public record SagaSpec : AddHandlerAndSagasRegistrationGenerator.Parser.BaseSpec
    {
        public SagaSpec(HandlerSpec handler, string sagaDataFullyQualifiedName, CorrelationPropertyMappingSpec correlationProperty, ImmutableEquatableArray<PropertyMappingSpec> propertyMappings)
            : base(handler)
        {
            SagaDataFullyQualifiedName = sagaDataFullyQualifiedName;
            CorrelationPropertyMapping = correlationProperty;
            PropertyMappings = propertyMappings;
            Handler = handler;
        }

        public string SagaDataFullyQualifiedName { get; }

        public CorrelationPropertyMappingSpec CorrelationPropertyMapping { get; }
        public ImmutableEquatableArray<PropertyMappingSpec> PropertyMappings { get; }
        public HandlerSpec Handler { get; }
    }

    public record PropertyMappingSpec(string MessageType, string MessageName, string MessagePropertyName, string MessagePropertyType);
    public readonly record struct CorrelationPropertyMappingSpec(string PropertyName, string PropertyType, string PropertyTypeMetadataName);

    public static SagaSpec? Parse(SemanticModel semanticModel, INamedTypeSymbol sagaType, CancellationToken cancellationToken = default)
    {
        // Extract saga data type from Saga<TSagaData>
        var sagaDataType = GetSagaDataType(sagaType);
        if (sagaDataType == null)
        {
            return null;
        }

        var sagaBaseSpec = Parser.Parse(semanticModel, sagaType, BaseParser.SpecKind.Saga, cancellationToken);
        var sagaDataFullyQualifiedName = sagaDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Analyze ConfigureHowToFindSaga to extract mappings
        var (correlationProperty, propertyMappings) = ExtractPropertyMappings(sagaType, semanticModel, cancellationToken);

        return correlationProperty is null ? null : new SagaSpec(sagaBaseSpec, sagaDataFullyQualifiedName, correlationProperty.Value, propertyMappings);
    }

    static INamedTypeSymbol? GetSagaDataType(INamedTypeSymbol sagaType)
    {
        // Find Saga<TSagaData> in the inheritance chain
        var baseType = sagaType.BaseType;
        while (baseType != null)
        {
            if (baseType is { IsGenericType: true, Name: "Saga", TypeArguments: [INamedTypeSymbol sagaDataType] })
            {
                return sagaDataType;
            }

            baseType = baseType.BaseType;
        }

        return null;
    }

    static (CorrelationPropertyMappingSpec? CorrelationProperty, ImmutableEquatableArray<PropertyMappingSpec> PropertyMappings) ExtractPropertyMappings(
        INamedTypeSymbol sagaType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var configureMethod = FindConfigureHowToFindSagaMethod(sagaType);

        // Get syntax node from method symbol (single declaration for overrides)
        var syntaxRef = configureMethod?.DeclaringSyntaxReferences.FirstOrDefault();

        var methodSyntax = syntaxRef?.GetSyntax(cancellationToken);
        if (methodSyntax is not MethodDeclarationSyntax methodDeclaration)
        {
            return (null, ImmutableEquatableArray<PropertyMappingSpec>.Empty);
        }

        // Get method body (block or expression body)
        SyntaxNode? methodBody = methodDeclaration.Body ?? (SyntaxNode?)methodDeclaration.ExpressionBody?.Expression;
        if (methodBody == null)
        {
            return (null, ImmutableEquatableArray<PropertyMappingSpec>.Empty);
        }

        var walker = new ConfigureMappingWalker(semanticModel, cancellationToken);
        walker.Visit(methodBody);

        if (walker.CorrelationPropertyMapping is null)
        {
            return (null, ImmutableEquatableArray<PropertyMappingSpec>.Empty);
        }

        // Sort mappings to ensure deterministic ordering
        return (walker.CorrelationPropertyMapping, walker.Mappings.OrderBy(m => m.MessageType, StringComparer.Ordinal)
            .ToImmutableEquatableArray());
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

    sealed class ConfigureMappingWalker(
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
        : CSharpSyntaxWalker
    {
        public List<PropertyMappingSpec> Mappings { get; } = [];
        public CorrelationPropertyMappingSpec? CorrelationPropertyMapping { get; private set; }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

            if (node.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: "MapSaga" } })
            {
                // This is a MapSaga call from MapSaga().ToMessage<TMessage>(...)
                // The pattern is: mapper.MapSaga(saga => saga.Prop).ToMessage<TMessage>(msg => msg.Prop)
                AnalyzeToSagaCall(node);
            }

            // Look for .ToMessage<TMessage>(...) calls (from MapSaga syntax)
            if (node.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "ToMessage" } })
            {
                // This is a ToMessage call from MapSaga().ToMessage<TMessage>(...)
                // The pattern is: mapper.MapSaga(saga => saga.Prop).ToMessage<TMessage>(msg => msg.Prop)
                AnalyzeMapSagaToMessageCall(node);
            }
        }

        void AnalyzeToSagaCall(InvocationExpressionSyntax mapSagaCall)
        {
            if (mapSagaCall.ArgumentList.Arguments.Count <= 0)
            {
                return;
            }

            if (mapSagaCall.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda)
            {
                return;
            }

            // Normalize body to a MemberAccessExpressionSyntax
            MemberAccessExpressionSyntax? memberAccess = lambda.Body switch
            {
                MemberAccessExpressionSyntax m => m,
                CastExpressionSyntax { Expression: MemberAccessExpressionSyntax castMember } => castMember,
                _ => null
            };

            if (memberAccess is null)
            {
                return;
            }

            // Property name (syntax)
            var propertyName = memberAccess.Name.Identifier.ValueText;
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            // Property symbol & type
            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);
            if (symbolInfo.Symbol is not IPropertySymbol propertySymbol)
            {
                return;
            }

            var propertyType = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            // SagaMapper.AllowedCorrelationPropertyTypes only allows primitive types so
            // using the metadata name is enough to create meaningful accessor names without having to TitleCase things.
            string propertySymbolMetadataName = propertySymbol.Type.MetadataName;
            CorrelationPropertyMapping = new CorrelationPropertyMappingSpec(propertyName, propertyType, propertySymbolMetadataName);
        }

        void AnalyzeMapSagaToMessageCall(InvocationExpressionSyntax toMessageCall)
        {
            if (toMessageCall.ArgumentList.Arguments.Count <= 0)
            {
                return;
            }

            if (toMessageCall.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda)
            {
                return;
            }

            // Normalize body to a MemberAccessExpressionSyntax
            MemberAccessExpressionSyntax? memberAccess = lambda.Body switch
            {
                MemberAccessExpressionSyntax m => m,
                CastExpressionSyntax { Expression: MemberAccessExpressionSyntax castMember } => castMember,
                _ => null
            };

            if (memberAccess is null)
            {
                return;
            }

            // Property name (syntax)
            var propertyName = memberAccess.Name.Identifier.ValueText;
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            // Message "variable" expression: the left side of "message.Property"
            var messageExpression = memberAccess.Expression;

            // Message type (symbol)
            var messageTypeSymbol = semanticModel.GetTypeInfo(messageExpression, cancellationToken).Type;
            if (messageTypeSymbol is null)
            {
                return;
            }

            var messageType = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var messageName = messageTypeSymbol.Name; // simple name, e.g. "SomeMessage"

            // Property symbol & type
            var symbolInfo = ModelExtensions.GetSymbolInfo(semanticModel, memberAccess, cancellationToken);
            if (symbolInfo.Symbol is not IPropertySymbol propertySymbol)
            {
                return;
            }

            var propertyType = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            Mappings.Add(new PropertyMappingSpec(messageType, messageName, propertyName, propertyType));
        }
    }
}