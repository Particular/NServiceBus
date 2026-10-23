#nullable enable

namespace NServiceBus.Core.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerRegistryAddAllAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [HandlerRegistryAddAllMustBeCalledDirectly];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(static context =>
        {
            if (context.Operation is IMethodReferenceOperation { Method: { } method } reference &&
                HandlerRegistryCompositionInterceptor.Parser.IsCompositionMethod(method))
            {
                context.ReportDiagnostic(Diagnostic.Create(HandlerRegistryAddAllMustBeCalledDirectly, reference.Syntax.GetLocation(), method.Name));
            }
        }, OperationKind.MethodReference);
    }

    static readonly DiagnosticDescriptor HandlerRegistryAddAllMustBeCalledDirectly = new(
        id: DiagnosticIds.HandlerRegistryAddAllMustBeCalledDirectly,
        title: "HandlerRegistry.AddAll must be called directly",
        messageFormat: "HandlerRegistry.{0} is replaced by the source generator only when called directly. Used as a delegate it throws at runtime.",
        category: "Code",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
