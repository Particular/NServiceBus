#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [HandlerAttributeMissing, HandlerAttributeMissingImmediate, HandlerAttributeMisplaced, HandlerAttributeMisplacedImmediate, HandlerAttributeOnNonHandlerType];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            if (!HandlerKnownTypes.TryGet(compilationContext.Compilation, out var knownTypes))
            {
                return;
            }

            var handlerTypes = new ConcurrentDictionary<INamedTypeSymbol, HandlerTypeSpec>(SymbolEqualityComparer.Default);
            var baseTypes = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } classType)
                {
                    return;
                }

                if (!classType.ImplementsGenericInterface(knownTypes.IHandleMessages) || classType.ImplementsGenericType(knownTypes.SagaBase))
                {
                    if (!classType.HasAttribute(knownTypes.HandlerAttribute))
                    {
                        return;
                    }

                    foreach (var location in classType.GetAttributeLocations(knownTypes.HandlerAttribute, context.CancellationToken))
                    {
                        if (location is not null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeOnNonHandlerType, location, classType.Name));
                        }
                    }

                    return;
                }

                if (classType.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
                {
                    baseTypes.TryAdd(baseType.OriginalDefinition, 0);
                }

                var attributeLocations = classType.GetAttributeLocations(knownTypes.HandlerAttribute, context.CancellationToken);
                // Abstract classes can't have the attribute
                if (classType.IsAbstract && !attributeLocations.IsDefaultOrEmpty)
                {
                    foreach (var location in attributeLocations)
                    {
                        if (location is not null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeMisplacedImmediate, location, classType.Name));
                        }
                    }
                }
                // concrete classes that are used as base classes
                else if (!classType.IsAbstract && !attributeLocations.IsDefaultOrEmpty)
                {
                    // this is supported because assembly scanning supported it
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
                            context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeMissingImmediate, location, classType.Name));
                        }
                    }
                }

                var info = new HandlerTypeSpec(classType.IsAbstract, attributeLocations);
                _ = handlerTypes.TryAdd(classType.OriginalDefinition, info);
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(context =>
            {
                foreach (var handler in handlerTypes)
                {
                    var type = handler.Key;
                    var handlerType = handler.Value;
                    var isLeaf = !handlerType.IsAbstract && !baseTypes.ContainsKey(type);

                    if (isLeaf)
                    {
                        if (handlerType.AttributeLocations.IsDefaultOrEmpty)
                        {
                            var location = type.GetClassIdentifierLocation(context.CancellationToken);
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeMissing, location, type.Name));
                            }
                        }

                        continue;
                    }

                    // Non abstract base classes that are handlers are allowed to have the attribute
                    // This makes things compatible with assembly scanning behavior.
                    if (!handlerType.IsAbstract && baseTypes.ContainsKey(type))
                    {
                        continue;
                    }

                    foreach (var location in handlerType.AttributeLocations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeMisplaced, location, type.Name));
                    }
                }
            });
        });
    }

    readonly record struct HandlerTypeSpec(bool IsAbstract, ImmutableArray<Location> AttributeLocations);

    static readonly DiagnosticDescriptor HandlerAttributeMissingImmediate = new(
        id: DiagnosticIds.HandlerAttributeMissing,
        title: "Mark message handlers with HandlerAttribute to enable source generation",
        messageFormat: "Mark message handler {0} with HandlerAttribute to enable generation of handler registration methods.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor HandlerAttributeMissing = new(
        id: DiagnosticIds.HandlerAttributeMissing,
        title: "Mark message handlers with HandlerAttribute to enable source generation",
        messageFormat: "Mark message handler {0} with HandlerAttribute to enable generation of handler registration methods.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor HandlerAttributeMisplacedImmediate = new(
        id: DiagnosticIds.HandlerAttributeMisplaced,
        title: "HandlerAttribute should be applied to concrete handler classes",
        messageFormat: "HandlerAttribute is applied to base class {0}, but should be placed on the concrete handler class.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor HandlerAttributeMisplaced = new(
        id: DiagnosticIds.HandlerAttributeMisplaced,
        title: "HandlerAttribute should be applied to concrete handler classes",
        messageFormat: "HandlerAttribute is applied to base class {0}, but should be placed on the concrete handler class.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor HandlerAttributeOnNonHandlerType = new(
        id: DiagnosticIds.HandlerAttributeOnNonHandler,
        title: "HandlerAttribute should be applied to classes implementing IHandleMessages",
        messageFormat: "HandlerAttribute is applied to base class {0}, but should be placed on a concrete handler class implementing IHandleMessages<T>.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}