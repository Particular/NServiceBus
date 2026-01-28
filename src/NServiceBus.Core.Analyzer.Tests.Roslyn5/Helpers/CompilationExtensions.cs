namespace NServiceBus.Core.Analyzer.Tests.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

static class CompilationExtensions
{
    extension(Compilation compilation)
    {
        public void Compile(bool throwOnFailure = true)
        {
            using var peStream = new MemoryStream();
            var emitResult = compilation.Emit(peStream);

            if (emitResult.Success)
            {
                return;
            }

            if (throwOnFailure)
            {
                throw new Exception("Compilation failed.");
            }

            Debug.WriteLine("Compilation failed.");
        }

        public async Task<IEnumerable<Diagnostic>> GetAnalyzerDiagnostics(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken = default)
        {
            var exceptions = new List<Exception>();

            var analysisOptions = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                (exception, _, __) => exceptions.Add(exception),
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: false);

            var diagnostics = await compilation
                .WithAnalyzers([analyzer], analysisOptions)
                .GetAnalyzerDiagnosticsAsync(cancellationToken);

            if (exceptions.Count != 0)
            {
                throw new AggregateException(exceptions);
            }

            return diagnostics
                .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
                .ThenBy(diagnostic => diagnostic.Id);
        }
    }
}