namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InterfaceLessHandlerCancellationTokenSuppressor : DiagnosticSuppressor
{
    static readonly SuppressionDescriptor SuppressPS0014Diagnostic = new(
        SupressionIds.InterfaceLessHandlerCancellationTokenSuppression,
        suppressedDiagnosticId: "PS0014",
        justification: "Interface-less handler CancellationToken parameters are bound from IMessageHandlerContext.CancellationToken.");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [SuppressPS0014Diagnostic];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Id != SuppressPS0014Diagnostic.SuppressedDiagnosticId)
            {
                continue;
            }

            if (diagnostic.Location.SourceTree is not { } sourceTree)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);
            var root = sourceTree.GetRoot(context.CancellationToken);
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var methodDeclaration = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodDeclaration is null)
            {
                continue;
            }

            if (semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            if (InterfaceLessHandlerCancellationTokenBinding.IsInterfaceLessHandlerWithBoundCancellationToken(methodSymbol, semanticModel.Compilation))
            {
                context.ReportSuppression(Suppression.Create(SuppressPS0014Diagnostic, diagnostic));
            }
        }
    }
}