namespace NServiceBus.Core.Analyzer.Tests.Helpers;

using System;
using System.Collections.Generic;
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

    public static async Task<IEnumerable<Diagnostic>> GetAnalyzerDiagnostics(this Compilation compilation, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken = default)
        => await compilation.GetAnalyzerDiagnostics(analyzer, null, cancellationToken);

    public static async Task<IEnumerable<Diagnostic>> GetAnalyzerDiagnostics(this Compilation compilation, DiagnosticAnalyzer analyzer, Dictionary<string, string> globalOptions, CancellationToken cancellationToken = default)
    {
        var exceptions = new List<Exception>();

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(globalOptions);
        var analyzerOptions = new AnalyzerOptions([], optionsProvider);

        var analysisOptions = new CompilationWithAnalyzersOptions(
            analyzerOptions,
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

    class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
    {
        readonly Dictionary<string, string> globalOptions = globalOptions ?? [];

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestAnalyzerConfigOptions(globalOptions);
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestAnalyzerConfigOptions(globalOptions);
        public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(globalOptions);
    }

    class TestAnalyzerConfigOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
    {
        readonly Dictionary<string, string> options = options ?? [];

        public override bool TryGetValue(string key, out string value) => options.TryGetValue(key, out value!);
    }
}