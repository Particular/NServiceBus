#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [HandlerAttributeMissing, HandlerAttributeMissingImmediate, HandlerAttributeMisplaced, HandlerAttributeMisplacedImmediate, ConventionBasedHandlerMissingAttribute, ConventionBasedHandlerMissingAttributeImmediate, ConventionBasedHandlerMisplacedAttribute, ConventionBasedHandlerMisplacedAttributeImmediate, HandlerAttributeOnNonHandlerType, ConventionBasedHandlerMixedStyleDescriptor, ConventionBasedHandlerNoAccessibleConstructorDescriptor, ConventionBasedHandlerAmbiguousConstructorDescriptor];

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

                var isInterfaceBasedHandler = classType.ImplementsGenericInterface(knownTypes.IHandleMessages);
                var isSaga = classType.ImplementsGenericType(knownTypes.SagaBase);
                var isConventionBasedHandler = !isSaga && ConventionBasedHandlerHelper.IsConventionBasedHandlerType(classType, knownTypes);

                if (!isInterfaceBasedHandler || isSaga)
                {
                    // A saga with [Handler] is always invalid
                    if (isSaga)
                    {
                        foreach (var location in classType.GetAttributeLocations(knownTypes.HandlerAttribute, context.CancellationToken))
                        {
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeOnNonHandlerType, location, classType.Name));
                            }
                        }
                        return;
                    }

                    // Non-handler class (no IHandleMessages<T>): valid only if it has convention-based Handle methods
                    if (!isConventionBasedHandler)
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

                    // Convention-based handlers participate in the same attribute-presence tracking
                    if (classType.BaseType is { SpecialType: not SpecialType.System_Object } handlerBaseType)
                    {
                        baseTypes.TryAdd(handlerBaseType.OriginalDefinition, 0);
                    }

                    var conventionBasedAttributeLocations = classType.GetAttributeLocations(knownTypes.HandlerAttribute, context.CancellationToken);
                    if (classType.IsAbstract && !conventionBasedAttributeLocations.IsDefaultOrEmpty)
                    {
                        foreach (var location in conventionBasedAttributeLocations)
                        {
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(ConventionBasedHandlerMisplacedAttributeImmediate, location, classType.Name));
                            }
                        }
                    }
                    else if (!classType.IsAbstract && conventionBasedAttributeLocations.IsDefaultOrEmpty)
                    {
                        var isUsedAsBase = baseTypes.ContainsKey(classType.OriginalDefinition);
                        var inheritsDirectlyFromObject = classType.BaseType?.SpecialType == SpecialType.System_Object;
                        var isDefinitelyLeaf = classType.IsSealed || !isUsedAsBase || inheritsDirectlyFromObject;

                        if (isDefinitelyLeaf)
                        {
                            var location = classType.GetClassIdentifierLocation(context.CancellationToken);
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(ConventionBasedHandlerMissingAttributeImmediate, location, classType.Name));
                            }
                        }
                    }

                    // Check for accessible constructor on non-static convention-based handlers
                    if (!classType.IsAbstract)
                    {
                        var hasInstanceHandleMethod = classType.GetMembers(ConventionBasedHandlerHelper.HandleMethodName)
                            .OfType<IMethodSymbol>()
                            .Any(m => !m.IsStatic && ConventionBasedHandlerHelper.IsValidConventionBasedHandleMethod(m, knownTypes, []));

                        if (hasInstanceHandleMethod && !ConventionBasedHandlerHelper.HasAccessibleConstructor(classType))
                        {
                            var classLocation = classType.GetClassIdentifierLocation(context.CancellationToken);
                            if (classLocation is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(ConventionBasedHandlerNoAccessibleConstructorDescriptor, classLocation, classType.Name));
                            }
                        }

                        // Check for ambiguous constructor selection
                        if (hasInstanceHandleMethod)
                        {
                            var ambiguousParamCount = ConventionBasedHandlerHelper.GetAmbiguousConstructorParameterCount(classType, knownTypes.ActivatorUtilitiesConstructorAttributeType);
                            if (ambiguousParamCount is not null)
                            {
                                var classLocation = classType.GetClassIdentifierLocation(context.CancellationToken);
                                if (classLocation is not null)
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(ConventionBasedHandlerAmbiguousConstructorDescriptor, classLocation, classType.Name, ambiguousParamCount));
                                }
                            }
                        }
                    }

                    var info2 = new HandlerTypeSpec(classType.IsAbstract, conventionBasedAttributeLocations, IsConventionBased: true);
                    _ = handlerTypes.TryAdd(classType.OriginalDefinition, info2);
                    return;
                }

                // Interface-based handler: check for mixed-style (also has convention-based Handle methods)
                if (ConventionBasedHandlerHelper.HasValidConventionBasedHandleMethods(classType, knownTypes))
                {
                    var classLocation = classType.GetClassIdentifierLocation(context.CancellationToken);
                    if (classLocation is not null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ConventionBasedHandlerMixedStyleDescriptor, classLocation, classType.Name));
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

                var info = new HandlerTypeSpec(classType.IsAbstract, attributeLocations, IsConventionBased: false);
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
                                context.ReportDiagnostic(Diagnostic.Create(handlerType.IsConventionBased ? ConventionBasedHandlerMissingAttribute : HandlerAttributeMissing, location, type.Name));
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
                        context.ReportDiagnostic(Diagnostic.Create(handlerType.IsConventionBased ? ConventionBasedHandlerMisplacedAttribute : HandlerAttributeMisplaced, location, type.Name));
                    }
                }
            });
        });
    }

    readonly record struct HandlerTypeSpec(bool IsAbstract, ImmutableArray<Location> AttributeLocations, bool IsConventionBased);

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

    static readonly DiagnosticDescriptor ConventionBasedHandlerMissingAttributeImmediate = new(
        id: DiagnosticIds.ConventionBasedHandlerMissingAttribute,
        title: "Mark convention-based handlers with HandlerAttribute to enable source generation",
        messageFormat: "Mark convention-based handler {0} with HandlerAttribute to enable generation of handler registration methods.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor ConventionBasedHandlerMissingAttribute = new(
        id: DiagnosticIds.ConventionBasedHandlerMissingAttribute,
        title: "Mark convention-based handlers with HandlerAttribute to enable source generation",
        messageFormat: "Mark convention-based handler {0} with HandlerAttribute to enable generation of handler registration methods.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor ConventionBasedHandlerMisplacedAttributeImmediate = new(
        id: DiagnosticIds.ConventionBasedHandlerMisplacedAttribute,
        title: "HandlerAttribute should be applied to concrete convention-based handler classes",
        messageFormat: "HandlerAttribute is applied to base class {0}, but should be placed on the concrete convention-based handler class.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor ConventionBasedHandlerMisplacedAttribute = new(
        id: DiagnosticIds.ConventionBasedHandlerMisplacedAttribute,
        title: "HandlerAttribute should be applied to concrete convention-based handler classes",
        messageFormat: "HandlerAttribute is applied to base class {0}, but should be placed on the concrete convention-based handler class.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor HandlerAttributeOnNonHandlerType = new(
        id: DiagnosticIds.HandlerAttributeOnNonHandler,
        title: "HandlerAttribute should be applied only to handler classes",
        messageFormat: "HandlerAttribute is applied to non-handler type {0}. Apply it only to concrete handler classes.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor ConventionBasedHandlerMixedStyleDescriptor = new(
        id: DiagnosticIds.ConventionBasedHandlerMixedStyle,
        title: "Handler class must use a single handler style",
        messageFormat: "Handler class {0} mixes interface-based (IHandleMessages<T>) and convention-based (Handle method) styles. Choose one approach: remove IHandleMessages<T> implementation and keep the Handle method, or implement IHandleMessages<T> and remove the Handle method.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor ConventionBasedHandlerNoAccessibleConstructorDescriptor = new(
        id: DiagnosticIds.ConventionBasedHandlerNoAccessibleConstructor,
        title: "Convention-based handler requires an accessible constructor",
        messageFormat: "Convention-based handler '{0}' has no accessible constructor. Make at least one constructor public or internal.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor ConventionBasedHandlerAmbiguousConstructorDescriptor = new(
        id: DiagnosticIds.ConventionBasedHandlerAmbiguousConstructor,
        title: "Convention-based handler has ambiguous constructor selection",
        messageFormat: "Convention-based handler '{0}' has multiple constructors with {1} parameters. Use [ActivatorUtilitiesConstructor] attribute to specify which constructor to use.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}