namespace NServiceBus.Core.Analyzer
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    class SagaMessageMapping
    {
        public SagaMessageMapping(TypeSyntax messageTypeSyntax, INamedTypeSymbol messageType, bool isHeader, ArgumentSyntax messageMappingExpression, LambdaExpressionSyntax toSagaSyntax)
        {
            MessageTypeSyntax = messageTypeSyntax;
            MessageType = messageType;
            IsHeader = isHeader;
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
            IsHeader = false;
            CorrelationId = correlationId;

            var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("msg"));
            var body = SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("msg"),
                        SyntaxFactory.IdentifierName(propertyName));

            var lambda = SyntaxFactory.SimpleLambdaExpression(parameter, body);

            MessageMappingExpression = SyntaxFactory.Argument(lambda).NormalizeWhitespace();
        }

        public static SagaMessageMapping CreateNewMapping(TypeSyntax messageTypeSyntax, string propertyName, string correlationId)
        {
            return new SagaMessageMapping(messageTypeSyntax, propertyName, correlationId);
        }

        public TypeSyntax MessageTypeSyntax { get; }
        public INamedTypeSymbol MessageType { get; set; }
        public bool IsHeader { get; }
        public ArgumentSyntax MessageMappingExpression { get; }
        public LambdaExpressionSyntax ToSagaSyntax { get; }
        public string CorrelationId { get; }
        public string NewMappingPropertyName { get; }

        public override string ToString()
        {
            return $"{nameof(SagaMessageMapping)}: {MessageMappingExpression.ToFullString()}, maps to sagaData.{CorrelationId}";
        }
    }
}
