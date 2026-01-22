namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerRegistryExtensionsAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [MultipleHandlerRegistryExtensions];

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

                var locations = GetAttributeLocations(classType, attributeType, context);
                if (!locations.IsDefaultOrEmpty)
                {
                    annotatedTypes.TryAdd(classType.OriginalDefinition, locations);
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

    static ImmutableArray<Location> GetAttributeLocations(INamedTypeSymbol classType, INamedTypeSymbol attributeType, SymbolAnalysisContext context)
    {
        var builder = ImmutableArray.CreateBuilder<Location>();

        foreach (var attribute in classType.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
            {
                continue;
            }

            if (attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is AttributeSyntax syntax)
            {
                builder.Add(syntax.GetLocation());
            }
        }

        return builder.ToImmutable();
    }

    static readonly DiagnosticDescriptor MultipleHandlerRegistryExtensions = new(
        id: DiagnosticIds.MultipleHandlerRegistryExtensions,
        title: "Multiple HandlerRegistryExtensionsAttribute declarations",
        messageFormat: "Only one HandlerRegistryExtensionsAttribute declaration is allowed per assembly.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);
}
