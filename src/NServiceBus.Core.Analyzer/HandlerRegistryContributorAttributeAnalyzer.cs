#nullable enable

namespace NServiceBus.Core.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerRegistryContributorAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [InvalidHandlerRegistryContributor];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        // Contributors are emitted by source generators, so the attribute usually sits in generated code
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var attributeType = compilationContext.Compilation.GetTypeByMetadataName("NServiceBus.HandlerRegistryContributorAttribute");
            var contributorInterface = compilationContext.Compilation.GetTypeByMetadataName(AddHandlerAndSagasRegistrationGenerator.Parser.ContributorInterfaceMetadataName);
            if (attributeType is null || contributorInterface is null)
            {
                return;
            }

            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } classType)
                {
                    return;
                }

                var locations = classType.GetAttributeLocations(attributeType, context.CancellationToken);
                if (locations.IsDefaultOrEmpty)
                {
                    return;
                }

                var problems = AddHandlerAndSagasRegistrationGenerator.Parser.GetContributorProblems(classType, contributorInterface, context.Compilation);
                if (problems.Count == 0)
                {
                    return;
                }

                var problemList = string.Join(", ", problems);
                foreach (var location in locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidHandlerRegistryContributor, location, classType.Name, problemList));
                }
            }, SymbolKind.NamedType);
        });
    }

    static readonly DiagnosticDescriptor InvalidHandlerRegistryContributor = new(
        id: DiagnosticIds.InvalidHandlerRegistryContributor,
        title: "Handler registry contributor is not valid",
        messageFormat: "The handler registry contributor {0} is not valid and is ignored: {1}.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true);
}
