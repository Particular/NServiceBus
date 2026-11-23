#nullable enable

namespace NServiceBus.Core.Analyzer;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class ExpressionSyntaxExtensions
{
    extension(ExpressionSyntax expression)
    {
        // Parentheses and the null-forgiving operator leave no trace in an expression tree, but a cast does.
        public ExpressionSyntax WithoutParenthesesOrSuppressions() =>
            expression switch
            {
                ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression.WithoutParenthesesOrSuppressions(),
                PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppression => suppression.Operand.WithoutParenthesesOrSuppressions(),
                _ => expression
            };
    }
}
