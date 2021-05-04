namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    static class CompilationExtensions
    {
        public static void Compile(this Compilation compilation)
        {
            using (var peStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(peStream);

                if (!emitResult.Success)
                {
                    throw new Exception("Compilation failed.");
                }
            }
        }

        public static async Task<IEnumerable<Diagnostic>> GetAnalyzerDiagnostics(this Compilation compilation, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken = default)
        {
            var exceptions = new List<Exception>();

            var analysisOptions = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                (exception, _, __) => exceptions.Add(exception),
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: false);


            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }

            return (await compilation
                .WithAnalyzers(ImmutableArray.Create(analyzer), analysisOptions)
                .GetAnalyzerDiagnosticsAsync(cancellationToken))
                .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
                .ThenBy(diagnostic => diagnostic.Id);
        }
    }
}
