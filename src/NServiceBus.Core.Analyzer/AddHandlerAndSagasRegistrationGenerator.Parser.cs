#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public partial class AddHandlerAndSagasRegistrationGenerator
{
    public static class Parser
    {
        const string HandlerRegistryExtensionsSuffix = "HandlerRegistryExtensions";

        public enum SpecKind
        {
            Handler,
            Saga
        }

        public record BaseSpec(string Name, string Namespace, string AssemblyName, string FullyQualifiedName, SpecKind Kind);

        // We override equality members to ignore the Regex property, and Regex doesn't reasonably support equality comparison
        public readonly record struct ReplacementSpec(string Replacement, string Expression, Regex Regex)
        {
            public bool Equals(ReplacementSpec other) => Replacement == other.Replacement && Expression == other.Expression;

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Replacement.GetHashCode() * 397) ^ Expression.GetHashCode();
                }
            }
        }

        public readonly record struct RootTypeSpec(string Namespace, string Visibility, string RootName, string ExtensionTypeName, ImmutableEquatableArray<ReplacementSpec> RegistrationMethodNamePatterns, bool IsExplicit)
        {
            public static RootTypeSpec CreateDefault(string assemblyId)
                => new("NServiceBus", "public", $"{assemblyId}Assembly", $"{assemblyId}{HandlerRegistryExtensionsSuffix}", ImmutableEquatableArray<ReplacementSpec>.Empty, false);
        }

        public static BaseSpec? Parse(GeneratorAttributeSyntaxContext context, SpecKind kind, CancellationToken cancellationToken = default) =>
            context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol ? null : Parse(namedTypeSymbol, kind, cancellationToken);

        public static BaseSpec Parse(INamedTypeSymbol namedTypeSymbol, SpecKind kind, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullyQualifiedName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var displayParts = namedTypeSymbol.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat);
            var handlerOrSagaName = string.Join("__", displayParts.Where(x => x.Kind == SymbolDisplayPartKind.ClassName));
            var handlerOrSagaNamespace = GetNamespace(displayParts);
            var assemblyName = namedTypeSymbol.ContainingAssembly?.Name ?? "Assembly";
            return new BaseSpec(Name: handlerOrSagaName, Namespace: handlerOrSagaNamespace, AssemblyName: assemblyName, FullyQualifiedName: fullyQualifiedName, Kind: kind);
        }

        public static RootTypeSpec? ParseRootTypeSpec(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default)
        {
            if (context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return null;
            }

            if (namedTypeSymbol.TypeKind != TypeKind.Class || !namedTypeSymbol.IsStatic || namedTypeSymbol.ContainingType is not null)
            {
                return null;
            }

            if (!IsPartial(namedTypeSymbol))
            {
                return null;
            }

            var namespaceName = namedTypeSymbol.ContainingNamespace is null || namedTypeSymbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : namedTypeSymbol.ContainingNamespace.ToDisplayString();
            var visibility = GetVisibility(namedTypeSymbol.DeclaredAccessibility);
            var entryPointName = GetEntryPointName(context);
            var rootName = entryPointName ?? string.Empty;
            var registrationMethodNamePatterns = GetRegistrationMethodNamePatterns(context);
            return new RootTypeSpec(namespaceName, visibility, rootName, namedTypeSymbol.Name, registrationMethodNamePatterns, true);
        }

        static string GetNamespace(ImmutableArray<SymbolDisplayPart> handlerType) => handlerType.Length == 0 ? string.Empty : string.Join(".", handlerType.Where(x => x.Kind == SymbolDisplayPartKind.NamespaceName));

        public static RootTypeSpec SelectRootTypeSpec(ImmutableArray<RootTypeSpec> explicitSpecs, string assemblyId)
        {
            if (explicitSpecs.Length == 0)
            {
                return RootTypeSpec.CreateDefault(assemblyId);
            }

            var selected = SelectPreferred(explicitSpecs);
            if (string.IsNullOrEmpty(selected.RootName))
            {
                return selected with { RootName = $"{assemblyId}Assembly" };
            }
            return selected;
        }

        static RootTypeSpec SelectPreferred(ImmutableArray<RootTypeSpec> specs)
        {
            var selected = specs[0];
            for (int i = 1; i < specs.Length; i++)
            {
                var candidate = specs[i];
                var namespaceComparison = StringComparer.Ordinal.Compare(candidate.Namespace, selected.Namespace);
                if (namespaceComparison < 0 ||
                    (namespaceComparison == 0 && StringComparer.Ordinal.Compare(candidate.ExtensionTypeName, selected.ExtensionTypeName) < 0))
                {
                    selected = candidate;
                }
            }

            return selected;
        }

        public static string GetRegistryRootName(string extensionTypeName) =>
            extensionTypeName.EndsWith(HandlerRegistryExtensionsSuffix, StringComparison.Ordinal)
                ? $"{extensionTypeName[..^HandlerRegistryExtensionsSuffix.Length]}Root"
                : $"{extensionTypeName}Root";

        static string? GetEntryPointName(GeneratorAttributeSyntaxContext context)
        {
            foreach (var attribute in context.Attributes)
            {
                foreach (var kvp in attribute.NamedArguments)
                {
                    if (kvp is { Key: "EntryPointName", Value.Value: string namedValue } &&
                        !string.IsNullOrWhiteSpace(namedValue))
                    {
                        return namedValue;
                    }
                }
            }

            return null;
        }

        static ImmutableEquatableArray<ReplacementSpec> GetRegistrationMethodNamePatterns(GeneratorAttributeSyntaxContext context)
        {
            foreach (var attribute in context.Attributes)
            {
                foreach (var kvp in attribute.NamedArguments)
                {
                    if (kvp.Key != "RegistrationMethodNamePatterns")
                    {
                        continue;
                    }

                    List<string>? registrationMethodNamePatterns = null;
                    if (kvp.Value is { Kind: TypedConstantKind.Array, IsNull: false })
                    {
                        registrationMethodNamePatterns ??= new List<string>(kvp.Value.Values.Length);
                        foreach (var value in kvp.Value.Values)
                        {
                            if (value.Value is string item && !string.IsNullOrWhiteSpace(item))
                            {
                                registrationMethodNamePatterns.Add(item);
                            }
                        }
                    }
                    else if (kvp.Value.Value is string namedValue && !string.IsNullOrWhiteSpace(namedValue))
                    {
                        registrationMethodNamePatterns ??= [namedValue];
                    }

                    if (registrationMethodNamePatterns is null)
                    {
                        return ImmutableEquatableArray<ReplacementSpec>.Empty;
                    }

                    List<ReplacementSpec>? replacementSpecs = null;
                    foreach (string registrationMethodNamePattern in registrationMethodNamePatterns)
                    {
                        if (!TrySplitPattern(registrationMethodNamePattern, out var pattern, out var replacement))
                        {
                            continue;
                        }

                        try
                        {
                            replacementSpecs ??= [];
                            var regex = new Regex(pattern);
                            replacementSpecs.Add(new ReplacementSpec(replacement, pattern, regex));
                        }
                        catch (ArgumentException)
                        {
                        }
                    }

                    if (replacementSpecs is not null)
                    {
                        return new ImmutableEquatableArray<ReplacementSpec>(replacementSpecs);
                    }
                }
            }

            return ImmutableEquatableArray<ReplacementSpec>.Empty;
        }

        static bool TrySplitPattern(string registrationMethodNamePattern, out string pattern, out string replacement)
        {
            if (string.IsNullOrEmpty(registrationMethodNamePattern))
            {
                pattern = string.Empty;
                replacement = string.Empty;
                return false;
            }

            var separatorIndex = registrationMethodNamePattern.IndexOf("=>", StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == registrationMethodNamePattern.Length - 2)
            {
                pattern = string.Empty;
                replacement = string.Empty;
                return false;
            }

            pattern = registrationMethodNamePattern[..separatorIndex];
            replacement = registrationMethodNamePattern[(separatorIndex + 2)..];
            return true;
        }

        static bool IsPartial(INamedTypeSymbol symbol)
        {
            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is ClassDeclarationSyntax classDeclarationSyntax &&
                    classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        static string GetVisibility(Accessibility accessibility) => accessibility == Accessibility.Public ? "public" : "internal";
    }
}