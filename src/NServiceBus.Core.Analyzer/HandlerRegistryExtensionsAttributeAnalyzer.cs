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
                if (annotatedTypes is { Count: <= 1 })
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

        if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax)
        {
            return false;
        }

        var arguments = attributeSyntax.ArgumentList?.Arguments;
        if (arguments is null || arguments.Value.Count == 0)
        {
            return false;
        }

        if (arguments.Value.Count > 0)
        {
            var firstArg = arguments.Value[0];
            if (firstArg.NameEquals is null && firstArg.Expression is LiteralExpressionSyntax { Token.ValueText: { } value })
            {
                entryPointName = value;
                location = firstArg.GetLocation();
                return true;
            }
        }

        foreach (var argument in arguments.Value)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText != "EntryPointName" ||
                argument.Expression is not LiteralExpressionSyntax { Token.ValueText: { } namedValue })
            {
                continue;
            }

            entryPointName = namedValue;
            location = argument.GetLocation();
            return true;
        }

        return false;
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