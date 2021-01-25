namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;
    using NUnit.Framework;

    public abstract class CodeFixVerifier : DiagnosticVerifier
    {
        protected abstract CodeFixProvider GetCodeFixProvider();

        protected virtual async Task VerifyFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
        {
            var analyzer = GetAnalyzer();
            var codeFixProvider = GetCodeFixProvider();

            var adhocProject = CreateProject(oldSource);

            var document = adhocProject.Documents.First();
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });
            var compilerDiagnostics = await GetCompilerDiagnostics(document);
            var attempts = analyzerDiagnostics.Length;

            for (int i = 0; i < attempts; ++i)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.RegisterCodeFixesAsync(context);

                if (!actions.Any())
                {
                    break;
                }

                if (codeFixIndex != null)
                {
                    document = await ApplyFix(document, actions.ElementAt((int)codeFixIndex));
                    break;
                }

                document = await ApplyFix(document, actions.ElementAt(0));
                analyzerDiagnostics = await GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });

                var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnostics(document));

                //check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(document.GetSyntaxRootAsync().Result, Formatter.Annotation, document.Project.Solution.Workspace));
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnostics(document));

                    Assert.True(false,
                        string.Format("Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
                            string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString())),
                            document.GetSyntaxRootAsync().Result.ToFullString()));
                }

                //check if there are analyzer diagnostics left after the code fix
                if (!analyzerDiagnostics.Any())
                {
                    break;
                }
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            var actual = await GetStringFromDocument(document);

            // Normalize line endings, just in case
            actual = actual.Replace("\r\n", "\n");
            newSource = newSource.Replace("\r\n", "\n");
            Assert.AreEqual(newSource, actual);
        }

        static IEnumerable<Diagnostic> GetNewDiagnostics(IEnumerable<Diagnostic> diagnostics, IEnumerable<Diagnostic> newDiagnostics)
        {
            var oldArray = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
            var newArray = newDiagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

            int oldIndex = 0;
            int newIndex = 0;

            while (newIndex < newArray.Length)
            {
                if (oldIndex < oldArray.Length && oldArray[oldIndex].Id == newArray[newIndex].Id)
                {
                    ++oldIndex;
                    ++newIndex;
                }
                else
                {
                    yield return newArray[newIndex++];
                }
            }
        }

        static async Task<IEnumerable<Diagnostic>> GetCompilerDiagnostics(Document document)
        {
            var model = await document.GetSemanticModelAsync();
            return model.GetDiagnostics();
        }

        static async Task<string> GetStringFromDocument(Document document)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation);
            var root = await simplifiedDoc.GetSyntaxRootAsync();
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
            return root.GetText().ToString();
        }

        static async Task<Document> ApplyFix(Document document, CodeAction codeAction)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            return solution.GetDocument(document.Id);
        }
    }
}
