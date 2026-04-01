namespace NServiceBus.Core.Analyzer.Fixes;

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Handlers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddConventionBasedHandleMethodFixer))]
public class AddConventionBasedHandleMethodFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.HandlerAttributeOnNonHandler];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var analyzerOptions = context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        if (analyzerOptions.TryGetValue("nservicebus_handler_style", out var handlerStyle) && string.Equals(handlerStyle, "IHandleMessages", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl is null || !HandlerFixerGuards.IsEmptyHandlerShell(classDecl))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Add convention-based Handle(MyMessage, ...)",
                    token => AddHandleMethod(context.Document, classDecl.SpanStart, token),
                    EquivalenceKey + handlerStyle),
                diagnostic);
        }
    }

    static async Task<Document> AddHandleMethod(Document document, int classPosition, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (editor.OriginalRoot.FindToken(classPosition).Parent?.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not { } classDeclaration || !HandlerKnownTypes.TryGet(editor.SemanticModel.Compilation, out var knownTypes))
        {
            return document;
        }

        var taskTypeSymbol = editor.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var cancellationTokenTypeSymbol = editor.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

        if (taskTypeSymbol is null || cancellationTokenTypeSymbol is null)
        {
            return document;
        }

        var workingClassDeclaration = HandlerFixerSyntaxHelpers.NormalizeClassBody(classDeclaration);

        var taskType = ((TypeSyntax)editor.Generator.TypeExpression(taskTypeSymbol))
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var messageType = SyntaxFactory.IdentifierName("MyMessage")
            .WithAdditionalAnnotations(RenameAnnotation.Create());

        var contextType = ((TypeSyntax)editor.Generator.TypeExpression(knownTypes.IMessageHandlerContext))
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var cancellationTokenType = ((TypeSyntax)editor.Generator.TypeExpression(cancellationTokenTypeSymbol))
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var messageParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
            .WithType(messageType);

        var contextParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
            .WithType(contextType);

        var cancellationTokenParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
            .WithType(cancellationTokenType)
            .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)));

        var completedTaskExpression = (ExpressionSyntax)editor.Generator.MemberAccessExpression(
            editor.Generator.TypeExpression(taskTypeSymbol),
            "CompletedTask");

        var awaitCompletedTask = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AwaitExpression(completedTaskExpression));

        var method = SyntaxFactory.MethodDeclaration(taskType, "Handle")
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        [messageParameter, contextParameter, cancellationTokenParameter])))
            .WithBody(SyntaxFactory.Block(awaitCompletedTask));

        method = HandlerFixerSyntaxHelpers.AnnotateIdentifierRename(method, "MyMessage", RenameAnnotation.Create())
            .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.AddImportsAnnotation);

        var updatedClass = workingClassDeclaration.AddMembers(method)
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(classDeclaration, updatedClass);
        return editor.GetChangedDocument();
    }

    static readonly string EquivalenceKey = $"{typeof(AddConventionBasedHandleMethodFixer).FullName}.AddMethod";
}