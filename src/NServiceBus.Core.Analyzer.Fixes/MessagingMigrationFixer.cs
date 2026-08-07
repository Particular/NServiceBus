#nullable enable

namespace NServiceBus.Core.Analyzer.Fixes;

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessagingMigrationFixer))]
public sealed class MessagingMigrationFixer : CodeFixProvider
{
    const string MessageTypeProperty = "MessageType";
    const string EquivalenceKey = "UseStronglyTypedMessageOverload";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [DiagnosticIds.UseGenericMessageType];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(MessageTypeProperty, out var messageType) ||
                string.IsNullOrWhiteSpace(messageType) ||
                root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                    .FirstAncestorOrSelf<InvocationExpressionSyntax>() is not { } invocation ||
                !CanAddTypeArgument(invocation.Expression))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use the strongly typed message overload",
                    cancellationToken => AddTypeArgument(
                        context.Document,
                        root,
                        invocation,
                        messageType!,
                        cancellationToken),
                    EquivalenceKey),
                diagnostic);
        }
    }

    static bool CanAddTypeArgument(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } => true,
        MemberBindingExpressionSyntax { Name: IdentifierNameSyntax } => true,
        IdentifierNameSyntax => true,
        _ => false
    };

    static Task<Document> AddTypeArgument(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        string messageType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var typeArgument = SyntaxFactory.ParseTypeName(messageType)
            .WithAdditionalAnnotations(Simplifier.Annotation);
        var typeArguments = SyntaxFactory.TypeArgumentList(
            SyntaxFactory.SingletonSeparatedList(typeArgument));

        ExpressionSyntax updatedExpression = invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } memberAccess =>
                memberAccess.WithName(
                    SyntaxFactory.GenericName(name.Identifier, typeArguments)
                        .WithTriviaFrom(name)),
            MemberBindingExpressionSyntax { Name: IdentifierNameSyntax name } memberBinding =>
                memberBinding.WithName(
                    SyntaxFactory.GenericName(name.Identifier, typeArguments)
                        .WithTriviaFrom(name)),
            IdentifierNameSyntax name =>
                SyntaxFactory.GenericName(name.Identifier, typeArguments)
                    .WithTriviaFrom(name),
            _ => invocation.Expression
        };

        var updatedInvocation = invocation.WithExpression(updatedExpression)
            .WithAdditionalAnnotations(Formatter.Annotation);
        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(invocation, updatedInvocation)));
    }
}
