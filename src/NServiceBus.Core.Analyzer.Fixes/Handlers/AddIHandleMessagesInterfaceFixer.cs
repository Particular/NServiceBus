namespace NServiceBus.Core.Analyzer.Fixes;

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
        if (editor.OriginalRoot.FindToken(classPosition).Parent?.FirstAncestorOrSelf<ClassDeclarationSyntax>() is not { } classDeclaration || !HandlerKnownTypes.TryGet(editor.SemanticModel.Compilation, out var knownTypes))
        {
            return document;
        }

        var interfaceType =
            SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("IHandleMessages"),
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName("MyMessage"))))
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

        var taskTypeSymbol = editor.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        if (taskTypeSymbol is null)
        {
            return document;
        }

        var taskType = ((TypeSyntax)editor.Generator.TypeExpression(taskTypeSymbol))
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var messageType = SyntaxFactory.IdentifierName("MyMessage")
            .WithAdditionalAnnotations(RenameAnnotation.Create());

        var contextType = ((TypeSyntax)editor.Generator.TypeExpression(knownTypes.IMessageHandlerContext))
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var messageParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
            .WithType(messageType);

        var contextParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
            .WithType(contextType);

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
                        [messageParameter, contextParameter])))
            .WithBody(SyntaxFactory.Block(awaitCompletedTask));

        method = AnnotateMyMessageRename(method)
            .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.AddImportsAnnotation);

        updatedClass = AnnotateMyMessageRename(updatedClass)
            .AddMembers(method)
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(classDeclaration, updatedClass);
        return editor.GetChangedDocument();
    }

    static MethodDeclarationSyntax AnnotateMyMessageRename(MethodDeclarationSyntax method)
    {
        var token = method
            .DescendantTokens()
            .FirstOrDefault(static t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == "MyMessage");

        return token.RawKind == 0 ? method : method.ReplaceToken(token, token.WithAdditionalAnnotations(RenameAnnotation.Create()));
    }

    static ClassDeclarationSyntax AnnotateMyMessageRename(ClassDeclarationSyntax classDeclaration)
    {
        var myMessageNode = classDeclaration
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .FirstOrDefault(static n => n.Identifier.ValueText == "MyMessage");

        return myMessageNode is null ? classDeclaration : classDeclaration.ReplaceNode(myMessageNode, myMessageNode.WithAdditionalAnnotations(RenameAnnotation.Create()));
    }

    static readonly string EquivalenceKey = $"{typeof(AddIHandleMessagesInterfaceFixer).FullName}.AddInterface";
}