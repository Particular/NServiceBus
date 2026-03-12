namespace NServiceBus.Core.Analyzer.Fixes;

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddConventionBasedHandleMethodFixer))]
public class AddConventionBasedHandleMethodFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.HandlerAttributeOnNonHandler];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not { } node)
            {
                continue;
            }

            var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl is null || !HandlerFixerGuards.IsEmptyHandlerShell(classDecl))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Add convention-based Handle(MyMessage, ...)",
                    token => AddHandleMethod(context.Document, classDecl.SpanStart, token),
                    EquivalenceKey),
                diagnostic);
        }
    }

    static async Task<Document> AddHandleMethod(Document document, int classPosition, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root?.FindToken(classPosition).Parent?.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not { } classDeclaration || SyntaxFactory.ParseMemberDeclaration(
                """
                public async Task Handle(MyMessage message, IMessageHandlerContext context, CancellationToken cancellationToken = default)
                {
                    await Task.CompletedTask;
                }
                """) is not MethodDeclarationSyntax method)
        {
            return document;
        }

        method = AnnotateMyMessageRename(method)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var updatedClass = classDeclaration.AddMembers(method)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(classDeclaration, updatedClass);
        if (newRoot is not CompilationUnitSyntax compilationUnit)
        {
            return document.WithSyntaxRoot(newRoot);
        }

        compilationUnit = AddUsingIfMissing(compilationUnit, "System.Threading.Tasks");
        compilationUnit = AddUsingIfMissing(compilationUnit, "System.Threading");
        newRoot = compilationUnit.WithAdditionalAnnotations(Formatter.Annotation);
        return document.WithSyntaxRoot(newRoot);
    }

    static MethodDeclarationSyntax AnnotateMyMessageRename(MethodDeclarationSyntax method)
    {
        var token = method
            .DescendantTokens()
            .FirstOrDefault(static t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == "MyMessage");

        return token.RawKind == 0 ? method : method.ReplaceToken(token, token.WithAdditionalAnnotations(RenameAnnotation.Create()));
    }

    static CompilationUnitSyntax AddUsingIfMissing(CompilationUnitSyntax compilationUnit, string namespaceName)
    {
        if (compilationUnit.Usings.Any(usingDirective =>
                usingDirective.Name?.ToString() == namespaceName))
        {
            return compilationUnit;
        }

        return compilationUnit.AddUsings(
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName)));
    }

    static readonly string EquivalenceKey = $"{typeof(AddConventionBasedHandleMethodFixer).FullName}.AddMethod";
}