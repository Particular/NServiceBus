#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SagaAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SagaAttributeMissing, SagaAttributeMissingImmediate, SagaAttributeMisplaced, SagaAttributeMisplacedImmediate, SagaAttributeOnNonSagaType];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            if (!SagaKnownTypes.TryGet(compilationContext.Compilation, out var knownTypes))
            {
                return;
            }

            var sagaTypes = new ConcurrentDictionary<INamedTypeSymbol, SagaTypeSpec>(SymbolEqualityComparer.Default);
            var baseTypes = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } classType)
                {
                    return;
                }

                if (!classType.ImplementsGenericType(knownTypes.SagaBase))
                {
                    if (!classType.HasAttribute(knownTypes.SagaAttribute))
                    {
                        return;
                    }

                    foreach (var location in classType.GetAttributeLocations(knownTypes.SagaAttribute, context.CancellationToken))
                    {
                        if (location is not null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(SagaAttributeOnNonSagaType, location, classType.Name));
                        }
                    }

                    return;
                }

                if (classType.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
                {
                    baseTypes.TryAdd(baseType.OriginalDefinition, 0);
                }

                var attributeLocations = classType.GetAttributeLocations(knownTypes.SagaAttribute, context.CancellationToken);
                // Abstract classes can't have the attribute
                if (classType.IsAbstract && !attributeLocations.IsDefaultOrEmpty)
                {
                    foreach (var location in attributeLocations)
                    {
                        if (location is not null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(SagaAttributeMisplacedImmediate, location, classType.Name));
                        }
                    }
                }
                // concrete classes that are used as base classes
                else if (!classType.IsAbstract && !attributeLocations.IsDefaultOrEmpty)
                {
                    var isUsedAsBase = baseTypes.ContainsKey(classType.OriginalDefinition);

                    if (isUsedAsBase)
                    {
                        foreach (var location in attributeLocations)
                        {
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(SagaAttributeMisplacedImmediate, location, classType.Name));
                            }
                        }
                    }
                }
                // concrete leaf classes without the attribute
                else if (!classType.IsAbstract && attributeLocations.IsDefaultOrEmpty)
                {
                    var isUsedAsBase = baseTypes.ContainsKey(classType.OriginalDefinition);

                    var inheritsDirectlyFromObject = classType.BaseType?.SpecialType == SpecialType.System_Object;
                    var isDefinitelyLeaf = classType.IsSealed || !isUsedAsBase || inheritsDirectlyFromObject;

                    if (isDefinitelyLeaf)
                    {
                        var location = classType.GetClassIdentifierLocation(context.CancellationToken);
                        if (location is not null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(SagaAttributeMissingImmediate, location, classType.Name));
                        }
                    }
                }

                var info = new SagaTypeSpec(classType.IsAbstract, attributeLocations);
                _ = sagaTypes.TryAdd(classType.OriginalDefinition, info);
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(context =>
            {
                foreach (var saga in sagaTypes)
                {
                    var type = saga.Key;
                    var sagaType = saga.Value;
                    var isLeaf = !sagaType.IsAbstract && !baseTypes.ContainsKey(type);

                    if (isLeaf)
                    {
                        if (sagaType.AttributeLocations.IsDefaultOrEmpty)
                        {
                            var location = type.GetClassIdentifierLocation(context.CancellationToken);
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(SagaAttributeMissing, location, type.Name));
                            }
                        }

                        continue;
                    }

                    foreach (var location in sagaType.AttributeLocations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(SagaAttributeMisplaced, location, type.Name));
                    }
                }
            });
        });
    }

    readonly record struct SagaTypeSpec(bool IsAbstract, ImmutableArray<Location> AttributeLocations);

    static readonly DiagnosticDescriptor SagaAttributeMissing = new(
        id: DiagnosticIds.SagaAttributeMissing,
        title: "SagaAttribute should be applied to sagas",
        messageFormat: "The saga {0} should be marked with SagaAttribute.",
        category: SagaDiagnostics.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor SagaAttributeMissingImmediate = new(
        id: DiagnosticIds.SagaAttributeMissing,
        title: "SagaAttribute should be applied to sagas",
        messageFormat: "The saga {0} should be marked with SagaAttribute.",
        category: SagaDiagnostics.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor SagaAttributeMisplaced = new(
        id: DiagnosticIds.SagaAttributeMisplaced,
        title: "SagaAttribute should be applied to concrete saga classes",
        messageFormat: "SagaAttribute is applied to {0}, but should be placed on the concrete saga class (not a base class).",
        category: SagaDiagnostics.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor SagaAttributeMisplacedImmediate = new(
        id: DiagnosticIds.SagaAttributeMisplaced,
        title: "SagaAttribute should be applied to concrete saga classes",
        messageFormat: "SagaAttribute is applied to {0}, but should be placed on the concrete saga class (not a base class).",
        category: SagaDiagnostics.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor SagaAttributeOnNonSagaType = new(
        id: DiagnosticIds.SagaAttributeOnNonSaga,
        title: "SagaAttribute should be applied to classes implementing Saga",
        messageFormat: "SagaAttribute is applied to {0}, but should be placed on a concrete saga class (not a base class) implementing Saga.",
        category: SagaDiagnostics.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}