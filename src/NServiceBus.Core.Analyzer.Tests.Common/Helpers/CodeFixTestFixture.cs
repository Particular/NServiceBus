namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    public abstract class CodeFixTestFixture<TAnalyzer, TCodeFix> : AnalyzerTestFixture<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        protected virtual async Task Assert(string original, string expected, bool fixMustCompile = true, CancellationToken cancellationToken = default)
        {
            var originalCodeFiles = SplitMarkupCodeIntoFiles(original);
            var expectedCodeFiles = SplitMarkupCodeIntoFiles(expected);

            var actual = await Fix(originalCodeFiles, fixMustCompile, cancellationToken);

            // normalize line endings, just in case
            for (int i = 0; i < originalCodeFiles.Length; i++)
            {
                actual[i] = actual[i].Replace("\r\n", "\n");
                expectedCodeFiles[i] = expectedCodeFiles[i].Replace("\r\n", "\n");
            }

            NUnit.Framework.Assert.AreEqual(expectedCodeFiles, actual);
        }

        async Task<string[]> Fix(string[] codeFiles, bool fixMustCompile, CancellationToken cancellationToken, IEnumerable<Diagnostic> originalCompilerDiagnostics = null)
        {
            var project = CreateProject(codeFiles);
            await WriteCode(project);

            var compilerDiagnostics = (await Task.WhenAll(project.Documents
                .Select(doc => doc.GetCompilerDiagnostics(cancellationToken))))
                .SelectMany(diagnostics => diagnostics);
            WriteCompilerDiagnostics(compilerDiagnostics);

            if (originalCompilerDiagnostics == null)
            {
                originalCompilerDiagnostics = compilerDiagnostics;
            }
            else if (fixMustCompile)
            {
                NUnit.Framework.CollectionAssert.AreEqual(originalCompilerDiagnostics, compilerDiagnostics, "Fix introduced new compiler diagnostics.");
            }

            var compilation = await project.GetCompilationAsync(cancellationToken);

            compilation.Compile(fixMustCompile);

            var analyzerDiagnostics = (await compilation.GetAnalyzerDiagnostics(new TAnalyzer(), cancellationToken)).ToList();
            WriteAnalyzerDiagnostics(analyzerDiagnostics);

            if (!analyzerDiagnostics.Any())
            {
                return codeFiles;
            }

            var actions = await project.GetCodeActions(new TCodeFix(), analyzerDiagnostics.First(), cancellationToken);

            if (!actions.Any())
            {
                return codeFiles;
            }

            if (!VerboseLogging)
            {
                Console.WriteLine("Applying code fix actions...");
            }

            var projectDocuments = project.Documents.ToArray();

            for (var i = 0; i < projectDocuments.Length; i++)
            {
                var document = projectDocuments[i];

                foreach (var action in actions.Where(action => action.Document.Name == document.Name))
                {
                    document = await document.ApplyChanges(action.Action, cancellationToken);
                }

                codeFiles[i] = await document.GetCode(cancellationToken);
            }

            return await Fix(codeFiles, fixMustCompile, cancellationToken, originalCompilerDiagnostics);
        }
    }
}
