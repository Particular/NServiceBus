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

static partial class Sagas
{
    public readonly record struct SagaSpecs(ImmutableEquatableArray<SagaSpec> Sagas);

    public record SagaSpec : AddHandlerAndSagasRegistrationGenerator.Parser.BaseSpec
    {
        public SagaSpec(HandlerSpec handler, string sagaDataFullyQualifiedName, ImmutableEquatableArray<PropertyMappingSpec> propertyMappings) : base(handler)
        {
            SagaDataFullyQualifiedName = sagaDataFullyQualifiedName;
            PropertyMappings = propertyMappings;
            Handler = handler;
        }

        public string SagaDataFullyQualifiedName { get; set; }
        public ImmutableEquatableArray<PropertyMappingSpec> PropertyMappings { get; }
        public HandlerSpec Handler { get; }
    }

    public record PropertyMappingSpec(string MessageType, string MessageName, string MessagePropertyName, string MessagePropertyType);

    public static SagaSpec? Parse(SemanticModel semanticModel, INamedTypeSymbol sagaType, CancellationToken cancellationToken = default)
    {
        // Extract saga data type from Saga<TSagaData>
        var sagaDataType = GetSagaDataType(sagaType);
        if (sagaDataType == null)
        {
            return null;
        }

        var handlerSpec = Parser.Parse(semanticModel, sagaType, cancellationToken);
        var sagaDataFullyQualifiedName = sagaDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Analyze ConfigureHowToFindSaga to extract mappings
        var propertyMappings = ExtractPropertyMappings(sagaType, semanticModel, cancellationToken);

        return new SagaSpec(handlerSpec, sagaDataFullyQualifiedName, propertyMappings);
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

    static ImmutableEquatableArray<PropertyMappingSpec> ExtractPropertyMappings(
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
            if (node.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "ToMessage" } })
            {
                // This is a ToMessage call from MapSaga().ToMessage<TMessage>(...)
                // The pattern is: mapper.MapSaga(saga => saga.Prop).ToMessage<TMessage>(msg => msg.Prop)
                AnalyzeMapSagaToMessageCall(node);
            }
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
            var messageTypeSymbol = ModelExtensions.GetTypeInfo(semanticModel, messageExpression, cancellationToken).Type;
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

            propertyMappings.Add(new PropertyMappingSpec(messageType, messageName, propertyName, propertyType));
        }
    }
}