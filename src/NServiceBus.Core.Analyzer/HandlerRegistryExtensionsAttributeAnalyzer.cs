namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerRegistryExtensionsAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [MultipleHandlerRegistryExtensions, HandlerRegistryExtensionsMustBePartial];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var attributeType = compilationContext.Compilation.GetTypeByMetadataName("NServiceBus.HandlerRegistryExtensionsAttribute");
            if (attributeType is null)
            {
                return;
            }

            var annotatedTypes = new ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<Location>>(SymbolEqualityComparer.Default);

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

                annotatedTypes.TryAdd(classType.OriginalDefinition, locations);
                if (classType.IsPartial())
                {
                    return;
                }

                foreach (var location in locations)
                {
                    context.ReportDiagnostic(Diagnostic.Create(HandlerRegistryExtensionsMustBePartial, location, classType.Name));
                }
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(context =>
            {
                if (annotatedTypes.Count <= 1)
                {
                    return;
                }

                var ordered = annotatedTypes
                    .Select(pair => (Type: pair.Key, pair.Value))
                    .OrderBy(item => item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
                    .ToArray();

                for (var i = 1; i < ordered.Length; i++)
                {
                    foreach (var location in ordered[i].Value)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(MultipleHandlerRegistryExtensions, location));
                    }
                }
            });
        });
    }

    static readonly DiagnosticDescriptor MultipleHandlerRegistryExtensions = new(
        id: DiagnosticIds.MultipleHandlerRegistryExtensions,
        title: "Multiple HandlerRegistryExtensionsAttribute declarations",
        messageFormat: "Only one HandlerRegistryExtensionsAttribute declaration is allowed per assembly.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor HandlerRegistryExtensionsMustBePartial = new(
        id: DiagnosticIds.HandlerRegistryExtensionsMustBePartial,
        title: "Handler registry extensions class must be partial",
        messageFormat: "The handler registry extensions class {0} must be partial.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true);
}