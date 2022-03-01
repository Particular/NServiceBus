namespace NServiceBus.Core.Analyzer
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    static class AnalysisContextExtensions
    {
        public static bool ContainsSyntax(this SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            return node.AncestorsAndSelf().Any(ancestor => ancestor == context.Node);
        }
    }
}
