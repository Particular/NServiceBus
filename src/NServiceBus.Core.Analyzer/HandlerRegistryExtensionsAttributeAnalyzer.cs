#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerRegistryExtensionsAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            MultipleHandlerRegistryExtensions,
            HandlerRegistryExtensionsMustBePartial,
            HandlerRegistryExtensionsEntryPointInvalid,
            HandlerRegistryExtensionsPatternFormatInvalid,
            HandlerRegistryExtensionsPatternRegexInvalid,
            MultipleHandlerRegistryExtensionsImmediate
        ];

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

                bool hasMultipleRegistrations = false;
                foreach (var location in EnumerateForMultipleRegistrationExtensionMethodLocations(annotatedTypes))
                {
                    hasMultipleRegistrations = true;
                    context.ReportDiagnostic(Diagnostic.Create(MultipleHandlerRegistryExtensionsImmediate, location));
                }

                if (hasMultipleRegistrations)
                {
                    // There is no point in doing the other heavy analysis if there are multiple registrations
                    return;
                }

                foreach (var attribute in classType.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                    {
                        continue;
                    }

                    if (TryGetEntryPointName(attribute, context.CancellationToken, out var entryPointName, out var location) &&
                        !string.IsNullOrWhiteSpace(entryPointName) && !IsValidPropertyIdentifier(entryPointName!))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(HandlerRegistryExtensionsEntryPointInvalid, location, entryPointName));
                    }

                    if (!TryGetRegistrationMethodNamePatterns(attribute, context.CancellationToken, out var patterns))
                    {
                        continue;
                    }

                    foreach (var (patternText, patternLocation) in patterns)
                    {
                        if (!TryGetPattern(patternText, out var regexPattern))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(HandlerRegistryExtensionsPatternFormatInvalid, patternLocation, patternText));
                            continue;
                        }

                        try
                        {
                            _ = new Regex(regexPattern);
                        }
                        catch (ArgumentException)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(HandlerRegistryExtensionsPatternRegexInvalid, patternLocation, regexPattern));
                        }
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

            compilationContext.RegisterCompilationEndAction(context => ReportMultipleHandlerRegistryExtensions(annotatedTypes, context));
        });
    }

    static void ReportMultipleHandlerRegistryExtensions(ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<Location>> annotatedTypes,
        CompilationAnalysisContext context)
    {
        foreach (var location in EnumerateForMultipleRegistrationExtensionMethodLocations(annotatedTypes))
        {
            context.ReportDiagnostic(Diagnostic.Create(MultipleHandlerRegistryExtensions, location));
        }
    }

    static IEnumerable<Location> EnumerateForMultipleRegistrationExtensionMethodLocations(ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<Location>> annotatedTypes)
    {
        if (annotatedTypes is { Count: <= 1 })
        {
            yield break;
        }

        var ordered = annotatedTypes
            .Select(pair => (Type: pair.Key, pair.Value))
            .OrderBy(item => item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
            .ToArray();

        for (var i = 1; i < ordered.Length; i++)
        {
            foreach (var location in ordered[i].Value)
            {
                yield return location;
            }
        }
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

        foreach (var argument in arguments.Value)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText != "EntryPointName" ||
                argument.Expression is not LiteralExpressionSyntax literalExpressionSyntax || literalExpressionSyntax is not { Token.ValueText: { } namedValue })
            {
                continue;
            }

            entryPointName = namedValue;
            location = literalExpressionSyntax.GetLocation();
            return true;
        }

        return false;
    }

    static bool TryGetRegistrationMethodNamePatterns(AttributeData attribute, CancellationToken cancellationToken, out ImmutableArray<(string Pattern, Location Location)> patterns)
    {
        patterns = [];

        if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax)
        {
            return false;
        }

        var arguments = attributeSyntax.ArgumentList?.Arguments;
        if (arguments is null || arguments.Value.Count == 0)
        {
            return false;
        }

        var builder = ImmutableArray.CreateBuilder<(string Pattern, Location Location)>();
        foreach (var argument in arguments.Value)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText != "RegistrationMethodNamePatterns")
            {
                continue;
            }

            ExtractPatterns(argument.Expression, builder);
        }

        if (builder.Count == 0)
        {
            return false;
        }

        patterns = builder.ToImmutable();
        return true;
    }

    static void ExtractPatterns(ExpressionSyntax expression, ImmutableArray<(string Pattern, Location Location)>.Builder builder)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax { Token.ValueText: { } value } literal:
                builder.Add((value, literal.GetLocation()));
                break;
            case ImplicitArrayCreationExpressionSyntax { Initializer.Expressions: { } expressions }:
                foreach (var item in expressions)
                {
                    ExtractPatterns(item, builder);
                }
                break;
            case ArrayCreationExpressionSyntax { Initializer.Expressions: { } expressions }:
                foreach (var item in expressions)
                {
                    ExtractPatterns(item, builder);
                }
                break;
            case CollectionExpressionSyntax { Elements: { } elements }:
                foreach (var element in elements)
                {
                    if (element is ExpressionElementSyntax { Expression: { } elementExpression })
                    {
                        ExtractPatterns(elementExpression, builder);
                    }
                }
                break;
            default:
                break;
        }
    }

    static bool IsValidPropertyIdentifier(string entryPointName)
    {
        if (!SyntaxFacts.IsValidIdentifier(entryPointName))
        {
            return false;
        }

        return entryPointName.StartsWith("@", StringComparison.Ordinal) || SyntaxFacts.GetKeywordKind(entryPointName) == SyntaxKind.None;
    }

    static readonly DiagnosticDescriptor MultipleHandlerRegistryExtensionsImmediate = new(
        id: DiagnosticIds.MultipleHandlerRegistryExtensions,
        title: "Multiple HandlerRegistryExtensionsAttribute declarations",
        messageFormat: "Only one HandlerRegistryExtensionsAttribute declaration is allowed per assembly.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true);

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

    static readonly DiagnosticDescriptor HandlerRegistryExtensionsPatternFormatInvalid = new(
        id: DiagnosticIds.HandlerRegistryExtensionsPatternFormatInvalid,
        title: "Handler registry registration pattern must use 'pattern=>replacement' format",
        messageFormat: "The registration method name pattern '{0}' must use the format 'pattern=>replacement'.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor HandlerRegistryExtensionsPatternRegexInvalid = new(
        id: DiagnosticIds.HandlerRegistryExtensionsPatternRegexInvalid,
        title: "Handler registry registration pattern regex must be valid",
        messageFormat: "The registration method name pattern regex '{0}' is not a valid regular expression.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true);

    static bool TryGetPattern(string registrationMethodNamePattern, out string pattern)
    {
        var separatorIndex = registrationMethodNamePattern.IndexOf("=>", StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == registrationMethodNamePattern.Length - 2)
        {
            pattern = string.Empty;
            return false;
        }

        pattern = registrationMethodNamePattern[..separatorIndex];
        return true;
    }
}