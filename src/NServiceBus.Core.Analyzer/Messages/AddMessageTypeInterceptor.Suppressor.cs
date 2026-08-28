namespace NServiceBus.Core.Analyzer.Messages;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AddMessageTypeInterceptorSuppressor : DiagnosticSuppressor
{
    static readonly SuppressionDescriptor SuppressRUCDiagnostic = new(
        SupressionIds.AddMessageTypeInterceptorSuppression,
        suppressedDiagnosticId: "IL2026",
        justification: "The AddMessageType method has been intercepted by a statically generated variant.");

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

            // The trim analyzer warns on the MemberAccessExpression since https://github.com/dotnet/runtime/pull/110086,
            // so the location can be the invocation or its child.
            var node = sourceTree.GetRoot().FindNode(location.SourceSpan) switch
            {
                InvocationExpressionSyntax s => s,
                { Parent: InvocationExpressionSyntax s } => s,
                _ => null,
            };

            if (node is null || !AddMessageTypeInterceptor.Parser.SyntaxLooksLikeAddMessageTypeMethod(node))
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);

            // Only suppress when an interceptor can actually be emitted for this call site. Calls with a generic
            // type parameter cannot be intercepted because the hierarchy cannot be computed statically, so they keep
            // the RequiresUnreferencedCode fallback warning.
            if (AddMessageTypeInterceptor.Parser.Parse(node, semanticModel, context.CancellationToken) is null)
            {
                continue;
            }

            context.ReportSuppression(Suppression.Create(SuppressRUCDiagnostic, diagnostic));
        }
    }

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [SuppressRUCDiagnostic];
}
