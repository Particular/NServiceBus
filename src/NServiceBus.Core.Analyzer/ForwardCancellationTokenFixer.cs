namespace NServiceBus.Core.Analyzer
{
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

    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ForwardCancellationTokenFixer))]
    public class ForwardCancellationTokenFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(ForwardCancellationTokenAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Since all the diagnostics have the same span
            // https://github.com/dotnet/roslyn-api-docs/blob/live/dotnet/xml/Microsoft.CodeAnalysis.CodeFixes/CodeFixContext.xml#L187
            // and the fixer only fixes one type of diagnostic,
            // we can assume there is only one diagnostic.
            // If there are duplicates then the analyzer is broken and its tests should catch that.
            var diagnostic = context.Diagnostics.First();

            var contextParamName = diagnostic.Properties["ContextParamName"];
            var methodName = diagnostic.Properties["MethodName"];
            var explicitParamName = diagnostic.Properties["ExplicitParameterName"];

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var startToken = root.FindToken(diagnostic.Location.SourceSpan.Start);
            var invocationSyntax = startToken.Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            var title = $"Forward {contextParamName}.CancellationToken to {methodName}";

            // Register a code action that will invoke the fix.
            var action = CodeAction.Create(
                title,
                token => ForwardCancellationToken(context.Document, invocationSyntax, contextParamName, explicitParamName, token),
                equivalenceKey: title);

            context.RegisterCodeFix(action, diagnostic);
        }

        static async Task<Document> ForwardCancellationToken(
            Document document,
            InvocationExpressionSyntax invocation,
            string contextParamName,
            string explicitParamName,
            CancellationToken cancellationToken)
        {
            // The context.CancellationToken part. This is always required.
            var memberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(contextParamName),
                SyntaxFactory.IdentifierName("CancellationToken"));

            ArgumentSyntax newArg;

            if (explicitParamName != null)
            {
                // Add the prefix `token:` if adding 1 argument would not be in the correct location.
                var nameColon = SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(explicitParamName));
                newArg = SyntaxFactory.Argument(nameColon, default, memberAccess);
            }
            else
            {
                newArg = SyntaxFactory.Argument(memberAccess);
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newArgList = invocation.ArgumentList.AddArguments(newArg);
            var newRoot = root.ReplaceNode(invocation.ArgumentList, newArgList);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}
