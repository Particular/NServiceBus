namespace NServiceBus.Core.Analyzer
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class AddCancellationTokenFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            MustImplementIHandleMessagesAnalyzer.NoCancellationTokenWarningDiagnostic.Id
        );

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var sourceLocation = diagnostic.Location;
            var diagnosticSpan = sourceLocation.SourceSpan;

            var startToken = root.FindToken(diagnosticSpan.Start);
            var methodDeclaration = startToken.Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            const string title = "Add CancellationToken";

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(title, ct =>
                AddCancellationToken(context.Document, methodDeclaration, ct), equivalenceKey: title), diagnostic);
        }

        private async Task<Document> AddCancellationToken(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken")).WithType(SyntaxFactory.ParseTypeName("CancellationToken"));
            var newParameterList = methodDeclaration.ParameterList.AddParameters(newParameter);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newRoot = root.ReplaceNode(methodDeclaration.ParameterList, newParameterList);

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}
