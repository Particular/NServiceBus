namespace NServiceBus.Core.Analyzer
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    class SagaMessageMapping
    {
        public SagaMessageMapping(TypeSyntax messageTypeSyntax, INamedTypeSymbol messageType, bool isHeaderMapping, ArgumentSyntax messageMappingExpression, LambdaExpressionSyntax toSagaSyntax)
        {
            MessageTypeSyntax = messageTypeSyntax;
            MessageType = messageType;
            IsHeaderMapping = isHeaderMapping;
            MessageMappingExpression = messageMappingExpression;
            ToSagaSyntax = toSagaSyntax;
            if (toSagaSyntax != null)
            {
                CorrelationId = (toSagaSyntax.Body as MemberAccessExpressionSyntax)?.Name?.Identifier.ValueText;
            }
        }

        SagaMessageMapping(TypeSyntax messageTypeSyntax, string propertyName, string correlationId)
        {
            MessageTypeSyntax = messageTypeSyntax;
            CorrelationId = correlationId;

            var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("msg"));
            var body = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("msg"),
                SyntaxFactory.IdentifierName(propertyName));

            var lambda = SyntaxFactory.SimpleLambdaExpression(parameter, body);

            MessageMappingExpression = SyntaxFactory.Argument(lambda).NormalizeWhitespace();
        }

        SagaMessageMapping(INamedTypeSymbol messageType)
        {
            MessageType = messageType;
            IsCustomFinder = true;
        }

        public static SagaMessageMapping CreateNewMapping(TypeSyntax messageTypeSyntax, string propertyName, string correlationId) => new(messageTypeSyntax, propertyName, correlationId);
        public static SagaMessageMapping CreateFinderMapping(INamedTypeSymbol messageType) => new(messageType);

        public TypeSyntax MessageTypeSyntax { get; }
        public INamedTypeSymbol MessageType { get; }
        public bool IsHeaderMapping { get; }
        public bool IsCustomFinder { get; }
        public ArgumentSyntax MessageMappingExpression { get; }
        public LambdaExpressionSyntax ToSagaSyntax { get; }
        public string CorrelationId { get; }

        public override string ToString() => $"{nameof(SagaMessageMapping)}: {MessageMappingExpression.ToFullString()}, maps to sagaData.{CorrelationId}";
    }
}