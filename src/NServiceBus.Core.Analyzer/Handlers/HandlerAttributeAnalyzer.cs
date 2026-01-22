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
        [HandlerAttributeMissing, HandlerAttributeMisplaced, HandlerAttributeOnNonHandlerType];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var iHandleMessages = compilationContext.Compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
            var handlerAttribute = compilationContext.Compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");

            if (iHandleMessages is null || handlerAttribute is null)
            {
                return;
            }

            var handlerTypes = new ConcurrentDictionary<INamedTypeSymbol, HandlerTypeSpec>(SymbolEqualityComparer.Default);
            var baseTypes = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);
            var knownTypes = new KnownTypeSpec(iHandleMessages, handlerAttribute);

            compilationContext.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } classType)
                {
                    return;
                }

                if (!classType.ImplementsGenericInterface(knownTypes.IHandleMessages))
                {
                    if (!classType.HasAttribute(handlerAttribute))
                    {
                        return;
                    }

                    foreach (var location in classType.GetAttributeLocations(handlerAttribute, context.CancellationToken))
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
                var info = new HandlerTypeSpec(classType.IsAbstract, attributeLocations);
                handlerTypes.TryAdd(classType.OriginalDefinition, info);
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

                    foreach (var location in handlerType.AttributeLocations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeMisplaced, location, type.Name));
                    }
                }
            });
        });
    }

    readonly record struct HandlerTypeSpec(bool IsAbstract, ImmutableArray<Location> AttributeLocations);
    readonly record struct KnownTypeSpec(INamedTypeSymbol IHandleMessages, INamedTypeSymbol HandlerAttribute);

    static readonly DiagnosticDescriptor HandlerAttributeMissing = new(
        id: DiagnosticIds.HandlerAttributeMissing,
        title: "HandlerAttribute should be applied to message handlers",
        messageFormat: "The message handler {0} should be marked with HandlerAttribute.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor HandlerAttributeMisplaced = new(
        id: DiagnosticIds.HandlerAttributeMisplaced,
        title: "HandlerAttribute should be applied to concrete handler classes",
        messageFormat: "HandlerAttribute is applied to {0}, but should be placed on the concrete handler class (not a base class).",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor HandlerAttributeOnNonHandlerType = new(
        id: DiagnosticIds.HandlerAttributeOnNonHandler,
        title: "HandlerAttribute should be applied to classes implementing",
        messageFormat: "HandlerAttribute is applied to {0}, but should be placed on a concrete handler class (not a base class) implementing IHandleMessages.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}