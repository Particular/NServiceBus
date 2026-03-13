namespace NServiceBus.Core.Analyzer.Fixes;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class HandlerFixerSyntaxHelpers
{
    public static ClassDeclarationSyntax NormalizeClassBody(ClassDeclarationSyntax classDeclaration)
    {
        if (!classDeclaration.OpenBraceToken.IsKind(SyntaxKind.None) &&
            !classDeclaration.CloseBraceToken.IsKind(SyntaxKind.None))
        {
            return classDeclaration;
        }

        return classDeclaration
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
            .WithSemicolonToken(default);
    }

    public static MethodDeclarationSyntax AnnotateIdentifierRename(MethodDeclarationSyntax methodDeclaration, string identifier, SyntaxAnnotation renameAnnotation)
    {
        var token = methodDeclaration
            .DescendantTokens()
            .FirstOrDefault(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == identifier);

        return token.RawKind == 0
            ? methodDeclaration
            : methodDeclaration.ReplaceToken(token, token.WithAdditionalAnnotations(renameAnnotation));
    }

    public static ClassDeclarationSyntax AnnotateIdentifierRename(ClassDeclarationSyntax classDeclaration, string identifier, SyntaxAnnotation renameAnnotation)
    {
        var identifierNode = classDeclaration
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .FirstOrDefault(identifierNode => identifierNode.Identifier.ValueText == identifier);

        return identifierNode is null
            ? classDeclaration
            : classDeclaration.ReplaceNode(identifierNode, identifierNode.WithAdditionalAnnotations(renameAnnotation));
    }
}