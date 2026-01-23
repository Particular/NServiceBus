#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerRegistryExtensionsAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [MultipleHandlerRegistryExtensions, HandlerRegistryExtensionsMustBePartial, HandlerRegistryExtensionsEntryPointInvalid];

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

                foreach (var attribute in classType.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                    {
                        continue;
                    }

                    if (!TryGetEntryPointName(attribute, context.CancellationToken, out var entryPointName, out var location))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entryPointName) || entryPointName is null)
                    {
                        continue;
                    }

                    if (!IsValidPropertyIdentifier(entryPointName))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(HandlerRegistryExtensionsEntryPointInvalid, location, entryPointName));
                    }
                }

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

    static bool TryGetEntryPointName(AttributeData attribute, CancellationToken cancellationToken, out string? entryPointName, out Location location)
    {
        entryPointName = null;
        location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None;

        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string ctorValue)
        {
            entryPointName = ctorValue;
            location = GetAttributeArgumentLocation(attribute, 0, null, cancellationToken) ?? location;
            return true;
        }

        foreach (var kvp in attribute.NamedArguments)
        {
            if (kvp.Key != "EntryPointName" || kvp.Value.Value is not string namedValue)
            {
                continue;
            }

            entryPointName = namedValue;
            location = GetAttributeArgumentLocation(attribute, null, kvp.Key, cancellationToken) ?? location;
            return true;
        }

        return false;
    }

    static Location? GetAttributeArgumentLocation(AttributeData attribute, int? positionalIndex, string? namedArgument, CancellationToken cancellationToken)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax)
        {
            return null;
        }

        var arguments = attributeSyntax.ArgumentList?.Arguments;
        if (arguments is null)
        {
            return null;
        }

        if (positionalIndex is not null && positionalIndex.Value < arguments.Value.Count)
        {
            return arguments.Value[positionalIndex.Value].GetLocation();
        }

        if (namedArgument is null)
        {
            return attributeSyntax.GetLocation();
        }

        foreach (var argument in arguments.Value)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText == namedArgument)
            {
                return argument.GetLocation();
            }
        }

        return attributeSyntax.GetLocation();
    }

    static bool IsValidPropertyIdentifier(string entryPointName)
    {
        if (!SyntaxFacts.IsValidIdentifier(entryPointName))
        {
            return false;
        }

        return entryPointName.StartsWith("@", StringComparison.Ordinal) || SyntaxFacts.GetKeywordKind(entryPointName) == SyntaxKind.None;
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

    static readonly DiagnosticDescriptor HandlerRegistryExtensionsEntryPointInvalid = new(
        id: DiagnosticIds.HandlerRegistryExtensionsEntryPointInvalid,
        title: "Handler registry entry point name must be a valid identifier",
        messageFormat: "The handler registry entry point name '{0}' must be a valid C# identifier.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true);
}