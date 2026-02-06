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
using NServiceBus.Core.Analyzer.Features;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FeatureDefaultsEnableFeatureFixer))]
public class FeatureDefaultsEnableFeatureFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.DoNotEnableFeaturesInDefaults];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root?.FindNode(context.Span) is not InvocationExpressionSyntax enableInvocation)
        {
            return;
        }

        var featureType = GetFeatureTypeArgument(enableInvocation)?.ToString();
        var title = featureType == null
            ? "Call Enable<TFeature>() from the constructor"
            : $"Call Enable<{featureType}>() from the constructor";

        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken => MoveEnableCallToConstructor(context.Document, enableInvocation, cancellationToken),
                EquivalenceKey),
            diagnostic);
    }

    static async Task<Document> MoveEnableCallToConstructor(
        Document document,
        InvocationExpressionSyntax enableInvocation,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;

        var lambda = enableInvocation.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();
        if (lambda is not
            {
                Parent: ArgumentSyntax
                {
                    Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax defaultsInvocation }
                }
            } ||
            semanticModel.GetSymbolInfo(defaultsInvocation, cancellationToken).Symbol is not IMethodSymbol defaultsSymbol)
        {
            return document;
        }

        var defaultsStatement = defaultsInvocation.FirstAncestorOrSelf<StatementSyntax>();
        if (defaultsStatement == null)
        {
            return document;
        }

        var typeArgument = GetFeatureTypeArgument(enableInvocation)?.WithoutTrivia();
        if (typeArgument == null)
        {
            return document;
        }

        var compilation = semanticModel.Compilation;
        var featureType = FeatureKnownTypes.TryGet(compilation, out var knownTypes) ? knownTypes.Feature : null;
        var defaultsDefinition = featureType?
            .GetMembers("Defaults")
            .OfType<IMethodSymbol>()
            .FirstOrDefault()?.OriginalDefinition;

        if (defaultsDefinition == null ||
            !SymbolEqualityComparer.IncludeNullability.Equals(defaultsSymbol.OriginalDefinition, defaultsDefinition))
        {
            return document;
        }

        bool removeDefaultsInvocation;
        var lambdaBodyBlock = GetLambdaBodyBlock(lambda);
        var invocationStatement = enableInvocation.FirstAncestorOrSelf<StatementSyntax>();

        if (lambdaBodyBlock == null || invocationStatement == null)
        {
            removeDefaultsInvocation = true;
        }
        else if (invocationStatement.Parent is BlockSyntax)
        {
            var topLevelStatements = lambdaBodyBlock.Statements;
            removeDefaultsInvocation = topLevelStatements.Count == 1 && topLevelStatements[0] == invocationStatement;
        }
        else
        {
            // Unable to safely remove the invocation (e.g. part of an if statement without braces).
            return document;
        }

        if (!removeDefaultsInvocation)
        {
            editor.RemoveNode(invocationStatement, SyntaxRemoveOptions.KeepNoTrivia);
        }

        var enableStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("Enable"),
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(typeArgument))),
                    SyntaxFactory.ArgumentList()))
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.InsertBefore(defaultsStatement, enableStatement);

        if (removeDefaultsInvocation)
        {
            editor.RemoveNode(defaultsStatement, SyntaxRemoveOptions.KeepExteriorTrivia);
        }

        var changed = editor.GetChangedDocument();
        var root = await changed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var formattedRoot = Formatter.Format(root, Formatter.Annotation, changed.Project.Solution.Workspace, cancellationToken: cancellationToken);
        return changed.WithSyntaxRoot(formattedRoot);
    }

    static TypeSyntax GetFeatureTypeArgument(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } => genericName.TypeArgumentList.Arguments.FirstOrDefault(),
            GenericNameSyntax genericName => genericName.TypeArgumentList.Arguments.FirstOrDefault(),
            _ => null,
        };

    static BlockSyntax GetLambdaBodyBlock(AnonymousFunctionExpressionSyntax lambda) =>
        lambda switch
        {
            SimpleLambdaExpressionSyntax { Body: BlockSyntax block } => block,
            ParenthesizedLambdaExpressionSyntax { Body: BlockSyntax block } => block,
            AnonymousMethodExpressionSyntax anonymous => anonymous.Block,
            _ => null,
        };

    static readonly string EquivalenceKey = typeof(FeatureDefaultsEnableFeatureFixer).FullName;
}
