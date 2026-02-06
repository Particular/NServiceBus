namespace NServiceBus.Core.Analyzer.Tests.Helpers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

// Currently, this mock analyzer does not support all trimming-related warnings.
// It only supports IL2026 for method invocations for now.
#pragma warning disable RS1001 // Yes we don't want it to be found
class MockTrimmingAnalyzer : DiagnosticAnalyzer
#pragma warning restore RS1001
{
    static readonly DiagnosticDescriptor IL2026Descriptor = new(
#pragma warning disable RS2008
        id: "IL2026",
#pragma warning restore RS2008
        title: "Using member with RequiresUnreferencedCodeAttribute",
        messageFormat: "Using member '{0}' which has 'RequiresUnreferencedCodeAttribute'",
        category: "Trimming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [IL2026Descriptor];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(operationContext =>
        {
            if (operationContext.Operation is not Microsoft.CodeAnalysis.Operations.IInvocationOperation invocation)
            {
                return;
            }

            var method = invocation.TargetMethod;
            if (!method.GetAttributes().Any(attr => attr.AttributeClass?.Name == "RequiresUnreferencedCodeAttribute"))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(IL2026Descriptor, invocation.Syntax.GetLocation(), method.Name);
            operationContext.ReportDiagnostic(diagnostic);
        }, OperationKind.Invocation);
    }
}