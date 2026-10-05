namespace NServiceBus.Core.Analyzer.Tests.Helpers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

// Currently, this mock analyzer does not support all trimming-related warnings.
// It only supports IL2026 and IL3050 for method invocations for now.
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

    static readonly DiagnosticDescriptor IL3050Descriptor = new(
#pragma warning disable RS2008
        id: "IL3050",
#pragma warning restore RS2008
        title: "Using member with RequiresDynamicCodeAttribute",
        messageFormat: "Using member '{0}' which has 'RequiresDynamicCodeAttribute'",
        category: "AOT",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [IL2026Descriptor, IL3050Descriptor];

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
            var attributes = method.GetAttributes();
            if (attributes.Any(attr => attr.AttributeClass?.Name == "RequiresUnreferencedCodeAttribute"))
            {
                operationContext.ReportDiagnostic(Diagnostic.Create(IL2026Descriptor, invocation.Syntax.GetLocation(), method.Name));
            }

            if (attributes.Any(attr => attr.AttributeClass?.Name == "RequiresDynamicCodeAttribute"))
            {
                operationContext.ReportDiagnostic(Diagnostic.Create(IL3050Descriptor, invocation.Syntax.GetLocation(), method.Name));
            }
        }, OperationKind.Invocation);
    }
}