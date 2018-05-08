using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NServiceBus.Core.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PublishAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor diagnostic = new DiagnosticDescriptor(
            "NServiceBus.Core.001",
            "TBD",
            "TBD",
            "TBD",
            DiagnosticSeverity.Error,
            true,
            "TBD");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(diagnostic);

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            if (context.Symbol.Name == "Publish") // TODO: replace this nonsense with an actual implementation
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostic, context.Symbol.Locations[0], context.Symbol.Name));
            }
        }
    }
}
