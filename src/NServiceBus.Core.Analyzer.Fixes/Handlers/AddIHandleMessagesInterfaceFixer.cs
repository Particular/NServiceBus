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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddIHandleMessagesInterfaceFixer))]
public class AddIHandleMessagesInterfaceFixer : CodeFixProvider
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
                    "Implement IHandleMessages<MyMessage>",
                    token => AddInterface(context.Document, classDecl.SpanStart, token),
                    EquivalenceKey),
                diagnostic);
        }
    }

    static async Task<Document> AddInterface(Document document, int classPosition, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (editor.OriginalRoot.FindToken(classPosition).Parent?.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not { } classDeclaration)
        {
            return document;
        }

        var interfaceType = SyntaxFactory.ParseTypeName("IHandleMessages<MyMessage>")
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Formatter.Annotation);
        var baseType = SyntaxFactory.SimpleBaseType(interfaceType);

        var updatedClass = classDeclaration.BaseList is null
            ? classDeclaration.WithBaseList(
                SyntaxFactory.BaseList(
                    SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType)))
            : classDeclaration.WithBaseList(
                classDeclaration.BaseList.WithTypes(classDeclaration.BaseList.Types.Add(baseType)));

        updatedClass = AnnotateMyMessageRename(updatedClass)
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(classDeclaration, updatedClass);
        return editor.GetChangedDocument();
    }

    static ClassDeclarationSyntax AnnotateMyMessageRename(ClassDeclarationSyntax classDeclaration)
    {
        var token = classDeclaration
            .DescendantTokens()
            .FirstOrDefault(static t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == "MyMessage");

        return token.RawKind == 0
            ? classDeclaration
            : classDeclaration.ReplaceToken(token, token.WithAdditionalAnnotations(RenameAnnotation.Create()));
    }

    static readonly string EquivalenceKey = $"{typeof(AddIHandleMessagesInterfaceFixer).FullName}.AddInterface";
}