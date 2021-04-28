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
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var contextVarName = diagnostic.Properties["ContextParamName"];
            var methodName = diagnostic.Properties["MethodName"];
            var explicitParameterName = diagnostic.Properties["ExplicitParameterName"];
            var sourceLocation = diagnostic.Location;
            var diagnosticSpan = sourceLocation.SourceSpan;

            var startToken = root.FindToken(diagnosticSpan.Start);
            var invocationSyntax = startToken.Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            string title = $"Forward {contextVarName}.CancellationToken to {methodName}";

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(title, ct =>
                AddCancellationToken(context.Document, invocationSyntax, contextVarName, explicitParameterName, ct), equivalenceKey: title), diagnostic);

        }

        static async Task<Document> AddCancellationToken(Document document, InvocationExpressionSyntax invocationSyntax, string contextVarName, string explicitParameterName, CancellationToken cancellationToken)
        {
            // The context.CancellationToken part. This is always required.
            var simpleMemberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(contextVarName), SyntaxFactory.IdentifierName("CancellationToken"));

            ArgumentSyntax newArg;
            if (explicitParameterName != null)
            {
                // Add the prefix `token: context.CancellationToken` if adding 1 argument would not be in the correct location
                var nameColon = SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(explicitParameterName));
                newArg = SyntaxFactory.Argument(nameColon, default, simpleMemberAccess);
            }
            else
            {
                newArg = SyntaxFactory.Argument(simpleMemberAccess);
            }
            var newArgList = invocationSyntax.ArgumentList.AddArguments(newArg);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newRoot = root.ReplaceNode(invocationSyntax.ArgumentList, newArgList);

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}
