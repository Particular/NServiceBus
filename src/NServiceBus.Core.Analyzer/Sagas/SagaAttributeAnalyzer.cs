#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SagaAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SagaAttributeMissing, SagaAttributeMisplaced, SagaAttributeOnNonSagaType];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var sagaBaseClass = compilationContext.Compilation.GetTypeByMetadataName("NServiceBus.Saga`1");
            var sagaAttribute = compilationContext.Compilation.GetTypeByMetadataName("NServiceBus.SagaAttribute");

            if (sagaBaseClass is null || sagaAttribute is null)
            {
                return;
            }

            var sagaTypes = new ConcurrentDictionary<INamedTypeSymbol, SagaTypeSpec>(SymbolEqualityComparer.Default);
            var baseTypes = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);
            var knownTypes = new KnownTypeSpec(sagaBaseClass, sagaAttribute);

            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } classType)
                {
                    return;
                }

                if (!classType.ImplementsGenericType(knownTypes.SagaBaseClass))
                {
                    if (!classType.HasAttribute(sagaAttribute))
                    {
                        return;
                    }

                    foreach (var location in classType.GetAttributeLocations(sagaAttribute, context.CancellationToken))
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
                var info = new SagaTypeSpec(classType.IsAbstract, attributeLocations);
                sagaTypes.TryAdd(classType.OriginalDefinition, info);
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(context =>
            {
                foreach (var saga in sagaTypes)
                {
                    var type = saga.Key;
                    var handlerType = saga.Value;
                    var isLeaf = !handlerType.IsAbstract && !baseTypes.ContainsKey(type);

                    if (isLeaf)
                    {
                        if (handlerType.AttributeLocations.IsDefaultOrEmpty)
                        {
                            var location = type.GetClassIdentifierLocation(context.CancellationToken);
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(SagaAttributeMissing, location, type.Name));
                            }
                        }

                        continue;
                    }

                    foreach (var location in handlerType.AttributeLocations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(SagaAttributeMisplaced, location, type.Name));
                    }
                }
            });
        });
    }

    readonly record struct SagaTypeSpec(bool IsAbstract, ImmutableArray<Location> AttributeLocations);
    readonly record struct KnownTypeSpec(INamedTypeSymbol SagaBaseClass, INamedTypeSymbol SagaAttribute);

    static readonly DiagnosticDescriptor SagaAttributeMissing = new(
        id: DiagnosticIds.SagaAttributeMissing,
        title: "SagaAttribute should be applied to sagas",
        messageFormat: "The saga {0} should be marked with SagaAttribute.",
        category: SagaDiagnostics.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor SagaAttributeMisplaced = new(
        id: DiagnosticIds.SagaAttributeMisplaced,
        title: "SagaAttribute should be applied to concrete saga classes",
        messageFormat: "SagaAttribute is applied to {0}, but should be placed on the concrete saga class (not a base class).",
        category: SagaDiagnostics.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor SagaAttributeOnNonSagaType = new(
        id: DiagnosticIds.SagaAttributeOnNonSaga,
        title: "SagaAttribute should be applied to classes implementing Saga",
        messageFormat: "SagaAttribute is applied to {0}, but should be placed on a concrete saga class (not a base class) implementing Saga.",
        category: SagaDiagnostics.DiagnosticCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}