namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
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
        public static void Compile(this Compilation compilation, bool throwOnFailure = true)
        {
            using (var peStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(peStream);

                if (!emitResult.Success)
                {
                    if (throwOnFailure)
                    {
                        throw new Exception("Compilation failed.");
                    }
                    else
                    {
                        Debug.WriteLine("Compilation failed.");
                    }
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

            var diagnostics = await compilation
                .WithAnalyzers(ImmutableArray.Create(analyzer), analysisOptions)
                .GetAnalyzerDiagnosticsAsync(cancellationToken);

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }

            return diagnostics
                .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
                .ThenBy(diagnostic => diagnostic.Id);
        }
    }
}
