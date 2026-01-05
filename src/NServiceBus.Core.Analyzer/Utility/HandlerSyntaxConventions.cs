namespace NServiceBus.Core.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class HandlerSyntaxConventions
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

    public static bool IsAddHandlerMethod(IMethodSymbol method) => method is
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

    const string AddHandlerMethodName = "AddHandler";
    const string AddHandlerClassName = "MessageHandlerRegistrationExtensions";
}