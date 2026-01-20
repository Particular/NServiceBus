#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

                    foreach (var location in GetAttributeLocations(classType, handlerAttribute, context))
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

                var attributeLocations = GetAttributeLocations(classType, knownTypes.HandlerAttribute, context);
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
                            var location = GetClassIdentifierLocation(type, context.CancellationToken);
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

    static ImmutableArray<Location> GetAttributeLocations(INamedTypeSymbol classType, INamedTypeSymbol handlerAttribute, SymbolAnalysisContext context)
    {
        var builder = ImmutableArray.CreateBuilder<Location>();

        foreach (var attribute in classType.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, handlerAttribute))
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


    static Location? GetClassIdentifierLocation(INamedTypeSymbol classType, CancellationToken cancellationToken)
    {
        foreach (var syntaxRef in classType.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax(cancellationToken) is ClassDeclarationSyntax classDecl)
            {
                return classDecl.Identifier.GetLocation();
            }
        }

        return null;
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
        title: "HandlerAttribute should be applied to leaf handler classes only",
        messageFormat: "HandlerAttribute is applied to {0}, but should be placed on leaf handler classes instead.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ["CompilationEnd"]);

    static readonly DiagnosticDescriptor HandlerAttributeOnNonHandlerType = new(
        id: DiagnosticIds.HandlerAttributeOnNonHandler,
        title: "HandlerAttribute should be applied to handler classes only",
        messageFormat: "HandlerAttribute is applied to {0}, but should be placed on a class implementing IHandleMessages.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}