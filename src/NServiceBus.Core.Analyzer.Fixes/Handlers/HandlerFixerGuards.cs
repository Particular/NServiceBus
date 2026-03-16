namespace NServiceBus.Core.Analyzer.Fixes;

using Microsoft.CodeAnalysis.CSharp.Syntax;

static class HandlerFixerGuards
{
    public static bool IsEmptyHandlerShell(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.BaseList is null &&
        classDeclaration.Members.Count == 0;
}