namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;

    static class DocumentExtensions
    {
        public static async Task<IEnumerable<Diagnostic>> GetCompilerDiagnostics(this Document document, CancellationToken cancellationToken = default) =>
            (await document.GetSemanticModelAsync(cancellationToken))
                .GetDiagnostics(cancellationToken: cancellationToken)
                .Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden)
                .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
                .ThenBy(diagnostic => diagnostic.Id);

        public static async Task<Tuple<Document, CodeAction>[]> GetCodeActions(this Project project, CodeFixProvider codeFix, Diagnostic diagnostic, CancellationToken cancellationToken = default)
        {
            var actions = new List<Tuple<Document, CodeAction>>();
            foreach (var document in project.Documents)
            {
                var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(new Tuple<Document, CodeAction>(document, action)), cancellationToken);
                await codeFix.RegisterCodeFixesAsync(context);
            }
            return actions.ToArray();
        }

        public static async Task<Document> ApplyChanges(this Document document, CodeAction codeAction, CancellationToken cancellationToken = default)
        {
            var operations = await codeAction.GetOperationsAsync(cancellationToken);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            return solution.GetDocument(document.Id);
        }

        public static async Task<string> GetCode(this Document document, CancellationToken cancellationToken = default)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken);
            var root = await simplifiedDoc.GetSyntaxRootAsync(cancellationToken);
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace, cancellationToken: cancellationToken);
            return root.GetText().ToString();
        }
    }
}
