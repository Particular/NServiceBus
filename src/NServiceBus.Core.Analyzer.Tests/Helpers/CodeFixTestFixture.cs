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
        protected new virtual async Task Assert(string original, string expected, CancellationToken cancellationToken = default)
        {
            var actual = await Fix(original, cancellationToken);

            // normalize line endings, just in case
            actual = actual.Replace("\r\n", "\n");
            expected = expected.Replace("\r\n", "\n");

            NUnit.Framework.Assert.AreEqual(expected, actual);
        }

        static async Task<string> Fix(string code, CancellationToken cancellationToken, IEnumerable<Diagnostic> originalCompilerDiagnostics = null)
        {
            WriteCode(code);

            var document = CreateDocument(code);

            var compilerDiagnostics = await document.GetCompilerDiagnostics(cancellationToken);
            WriteCompilerDiagnostics(compilerDiagnostics);

            if (originalCompilerDiagnostics == null)
            {
                originalCompilerDiagnostics = compilerDiagnostics;
            }
            else
            {
                NUnit.Framework.CollectionAssert.AreEqual(originalCompilerDiagnostics, compilerDiagnostics, "Fix introduced new compiler diagnostics.");
            }

            var compilation = await document.Project.GetCompilationAsync(cancellationToken);
            compilation.Compile();

            var analyzerDiagnostics = (await compilation.GetAnalyzerDiagnostics(new TAnalyzer(), cancellationToken)).ToList();
            WriteAnalyzerDiagnostics(analyzerDiagnostics);

            if (!analyzerDiagnostics.Any())
            {
                return code;
            }

            var actions = await document.GetCodeActions(new TCodeFix(), analyzerDiagnostics.First(), cancellationToken);

            if (!actions.Any())
            {
                return code;
            }

            Console.WriteLine("Applying code fix actions...");
            foreach (var action in actions)
            {
                document = await document.ApplyChanges(action, cancellationToken);
            }

            code = await document.GetCode(cancellationToken);

            return await Fix(code, cancellationToken, originalCompilerDiagnostics);
        }
    }
}
