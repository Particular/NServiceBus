#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        public readonly record struct RootTypeSpec(string Namespace, string Visibility, string RootName, string ExtensionTypeName, ImmutableEquatableArray<string> RegistrationMethodNamePatterns, bool IsExplicit)
        {
            public static RootTypeSpec CreateDefault(string assemblyId)
                => new("NServiceBus", "public", $"{assemblyId}Assembly", $"{assemblyId}{HandlerRegistryExtensionsSuffix}", ImmutableEquatableArray<string>.Empty, false);
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

        static ImmutableEquatableArray<string> GetRegistrationMethodNamePatterns(GeneratorAttributeSyntaxContext context)
        {
            foreach (var attribute in context.Attributes)
            {
                foreach (var kvp in attribute.NamedArguments)
                {
                    if (kvp.Key != "RegistrationMethodNamePatterns")
                    {
                        continue;
                    }

                    if (kvp.Value is { Kind: TypedConstantKind.Array, IsNull: false })
                    {
                        var patterns = new List<string>();
                        foreach (var value in kvp.Value.Values)
                        {
                            if (value.Value is string item && !string.IsNullOrWhiteSpace(item))
                            {
                                patterns.Add(item);
                            }
                        }
                        return new ImmutableEquatableArray<string>(patterns);
                    }

                    if (kvp.Value.Value is string namedValue && !string.IsNullOrWhiteSpace(namedValue))
                    {
                        return new ImmutableEquatableArray<string>([namedValue]);
                    }
                }
            }

            return ImmutableEquatableArray<string>.Empty;
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