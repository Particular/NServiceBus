namespace NServiceBus.Core.Analyzer.Sagas;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AddSagaInterceptorSuppressor : DiagnosticSuppressor
{
    static readonly SuppressionDescriptor SuppressRUCDiagnostic = new(
        SupressionIds.AddSagaInterceptorSuppression,
        suppressedDiagnosticId: "IL2026",
        justification: "The AddSaga method has been intercepted by a statically generated variant.");

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Id != SuppressRUCDiagnostic.SuppressedDiagnosticId)
            {
                continue;
            }

            var location = diagnostic.AdditionalLocations.Count > 0 ? diagnostic.AdditionalLocations[0] : diagnostic.Location;
            if (location.SourceTree is not { } sourceTree)
            {
                continue;
            }

            // The trim analyzer changed from warning on the InvocationExpression to the MemberAccessExpression in https://github.com/dotnet/runtime/pull/110086.
            // To account for this, we need to check if the location is an InvocationExpression or a child of an InvocationExpression.
            var node = sourceTree.GetRoot().FindNode(location.SourceSpan) switch
            {
                InvocationExpressionSyntax s => s,
                { Parent: InvocationExpressionSyntax s } => s,
                _ => null,
            };

            if (node is null || !AddSagaInterceptor.Parser.SyntaxLooksLikeAddSagaMethod(node))
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);
            var operation = semanticModel.GetOperation(node, context.CancellationToken);
            if (operation is IInvocationOperation { TargetMethod: { } methodSymbol } && AddSagaInterceptor.Parser.IsAddSagaMethod(methodSymbol))
            {
                context.ReportSuppression(Suppression.Create(SuppressRUCDiagnostic, diagnostic));
            }
        }
    }

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [SuppressRUCDiagnostic];
}