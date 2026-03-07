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
        [HandlerAttributeMissing, HandlerAttributeMissingImmediate, HandlerAttributeMisplaced, HandlerAttributeMisplacedImmediate, HandlerAttributeMissingInterfaceLess, HandlerAttributeMissingInterfaceLessImmediate, HandlerAttributeMisplacedInterfaceLess, HandlerAttributeMisplacedInterfaceLessImmediate, HandlerAttributeOnNonHandlerType, HandlerAttributeMixedStyleDescriptor];

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
                var isInterfaceLessHandler = !isSaga && IsInterfaceLessHandlerType(classType);

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

                    // Non-handler class (no IHandleMessages<T>): valid only if it has interface-less Handle methods
                    if (!isInterfaceLessHandler)
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

                    // Interface-less handlers participate in the same attribute-presence tracking
                    if (classType.BaseType is { SpecialType: not SpecialType.System_Object } handlerBaseType)
                    {
                        baseTypes.TryAdd(handlerBaseType.OriginalDefinition, 0);
                    }

                    var interfaceLessAttributeLocations = classType.GetAttributeLocations(knownTypes.HandlerAttribute, context.CancellationToken);
                    if (classType.IsAbstract && !interfaceLessAttributeLocations.IsDefaultOrEmpty)
                    {
                        foreach (var location in interfaceLessAttributeLocations)
                        {
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeMisplacedInterfaceLessImmediate, location, classType.Name));
                            }
                        }
                    }
                    else if (!classType.IsAbstract && interfaceLessAttributeLocations.IsDefaultOrEmpty)
                    {
                        var isUsedAsBase = baseTypes.ContainsKey(classType.OriginalDefinition);
                        var inheritsDirectlyFromObject = classType.BaseType?.SpecialType == SpecialType.System_Object;
                        var isDefinitelyLeaf = classType.IsSealed || !isUsedAsBase || inheritsDirectlyFromObject;

                        if (isDefinitelyLeaf)
                        {
                            var location = classType.GetClassIdentifierLocation(context.CancellationToken);
                            if (location is not null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeMissingInterfaceLessImmediate, location, classType.Name));
                            }
                        }
                    }

                    var info2 = new HandlerTypeSpec(classType.IsAbstract, interfaceLessAttributeLocations, IsInterfaceLess: true);
                    _ = handlerTypes.TryAdd(classType.OriginalDefinition, info2);
                    return;
                }

                // Interface-based handler: check for mixed-style (also has interface-less Handle methods)
                if (HasMixedStyleHandleMethods(classType))
                {
                    var classLocation = classType.GetClassIdentifierLocation(context.CancellationToken);
                    if (classLocation is not null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(HandlerAttributeMixedStyleDescriptor, classLocation, classType.Name));
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

                var info = new HandlerTypeSpec(classType.IsAbstract, attributeLocations, IsInterfaceLess: false);
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
                                context.ReportDiagnostic(Diagnostic.Create(handlerType.IsInterfaceLess ? HandlerAttributeMissingInterfaceLess : HandlerAttributeMissing, location, type.Name));
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
                        context.ReportDiagnostic(Diagnostic.Create(handlerType.IsInterfaceLess ? HandlerAttributeMisplacedInterfaceLess : HandlerAttributeMisplaced, location, type.Name));
                    }
                }
            });
        });
    }

    readonly record struct HandlerTypeSpec(bool IsAbstract, ImmutableArray<Location> AttributeLocations, bool IsInterfaceLess);

    static bool IsInterfaceLessHandlerType(INamedTypeSymbol classType)
    {
        for (var current = classType; current is not null; current = current.BaseType)
        {
            if (HasValidInterfaceLessHandleMethods(current))
            {
                return true;
            }
        }

        return false;
    }

    static bool HasValidInterfaceLessHandleMethods(INamedTypeSymbol classType)
    {
        var interfaceMessageTypes = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        foreach (var iface in classType.AllInterfaces)
        {
            if (iface is { Name: "IHandleMessages" or "IHandleTimeouts" or "IAmStartedByMessages", IsGenericType: true } &&
                iface.TypeArguments[0] is INamedTypeSymbol msgType)
            {
                interfaceMessageTypes.Add(msgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        foreach (var member in classType.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            if (method.Name != "Handle" ||
                method.DeclaredAccessibility != Accessibility.Public ||
                method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
                method.Parameters.Length < 2)
            {
                continue;
            }

            if (method.Parameters[0].Type is not INamedTypeSymbol firstParamType)
            {
                continue;
            }

            if (!IsIMessageHandlerContext(method.Parameters[1].Type))
            {
                continue;
            }

            if (!IsSupportedHandlerReturnType(method.ReturnType))
            {
                continue;
            }

            var firstParamFqn = firstParamType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (method.Parameters.Length == 2 && interfaceMessageTypes.Contains(firstParamFqn))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    static bool HasMixedStyleHandleMethods(INamedTypeSymbol classType)
    {
        // Collect message types already covered by IHandleMessages<T> interfaces
        var interfaceMessageTypes = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        foreach (var iface in classType.AllInterfaces)
        {
            if (iface is { Name: "IHandleMessages" or "IHandleTimeouts" or "IAmStartedByMessages", IsGenericType: true } &&
                iface.TypeArguments[0] is INamedTypeSymbol msgType)
            {
                interfaceMessageTypes.Add(msgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        foreach (var member in classType.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            if (method.Name != "Handle" ||
                method.DeclaredAccessibility != Accessibility.Public ||
                method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
                method.Parameters.Length < 2)
            {
                continue;
            }

            if (!IsIMessageHandlerContext(method.Parameters[1].Type))
            {
                continue;
            }

            if (!IsSupportedHandlerReturnType(method.ReturnType))
            {
                continue;
            }

            if (method.Parameters[0].Type is not INamedTypeSymbol firstParamType)
            {
                continue;
            }

            var firstParamFqn = firstParamType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // This method is interface-less if: has extra params OR its message type is not in IHandleMessages<T> interfaces
            bool isInterfaceLess = method.Parameters.Length > 2 || !interfaceMessageTypes.Contains(firstParamFqn);
            if (isInterfaceLess)
            {
                return true;
            }
        }

        return false;
    }

    static bool IsIMessageHandlerContext(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { Name: "IMessageHandlerContext", ContainingNamespace: { Name: "NServiceBus", ContainingNamespace.IsGlobalNamespace: true } };
    }

    static bool IsSupportedHandlerReturnType(ITypeSymbol type) =>
        type.Name is "Task";

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

    static readonly DiagnosticDescriptor HandlerAttributeMissingInterfaceLessImmediate = new(
        id: DiagnosticIds.HandlerAttributeMissingInterfaceLess,
        title: "Mark interface-less handlers with HandlerAttribute to enable source generation",
        messageFormat: "Mark interface-less handler {0} with HandlerAttribute to enable generation of handler registration methods.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor HandlerAttributeMissingInterfaceLess = new(
        id: DiagnosticIds.HandlerAttributeMissingInterfaceLess,
        title: "Mark interface-less handlers with HandlerAttribute to enable source generation",
        messageFormat: "Mark interface-less handler {0} with HandlerAttribute to enable generation of handler registration methods.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor HandlerAttributeMisplacedInterfaceLessImmediate = new(
        id: DiagnosticIds.HandlerAttributeMisplacedInterfaceLess,
        title: "HandlerAttribute should be applied to concrete interface-less handler classes",
        messageFormat: "HandlerAttribute is applied to base class {0}, but should be placed on the concrete interface-less handler class.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor HandlerAttributeMisplacedInterfaceLess = new(
        id: DiagnosticIds.HandlerAttributeMisplacedInterfaceLess,
        title: "HandlerAttribute should be applied to concrete interface-less handler classes",
        messageFormat: "HandlerAttribute is applied to base class {0}, but should be placed on the concrete interface-less handler class.",
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

    static readonly DiagnosticDescriptor HandlerAttributeMixedStyleDescriptor = new(
        id: DiagnosticIds.HandlerAttributeMixedStyle,
        title: "Handler class must use a single handler style",
        messageFormat: "Handler class {0} mixes interface-based (IHandleMessages<T>) and interface-less (Handle method) styles. Split into separate classes — one per style.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
