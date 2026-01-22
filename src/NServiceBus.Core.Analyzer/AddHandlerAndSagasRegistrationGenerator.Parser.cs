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
    internal static class Parser
    {
        internal record BaseSpec(string Name, string Namespace, string AssemblyName, string FullyQualifiedName);
        internal readonly record struct RootTypeSpec(string Namespace, string Visibility)
        {
            public static RootTypeSpec Default { get; } = new("NServiceBus", "public");
        }

        public static BaseSpec? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default) => context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol ? null : Parse(namedTypeSymbol);

        public static BaseSpec Parse(INamedTypeSymbol namedTypeSymbol)
        {
            var fullyQualifiedName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var displayParts = namedTypeSymbol.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat);
            var handlerOrSagaName = string.Join(string.Empty, displayParts.Where(x => x.Kind == SymbolDisplayPartKind.ClassName));
            var handlerOrSagaNamespace = GetNamespace(displayParts);
            var assemblyName = namedTypeSymbol.ContainingAssembly?.Name ?? "Assembly";
            return new BaseSpec(Name: handlerOrSagaName, Namespace: handlerOrSagaNamespace, AssemblyName: assemblyName, FullyQualifiedName: fullyQualifiedName);
        }

        public static RootTypeSpec? TryGetRootTypeSpec(ClassDeclarationSyntax classDeclarationSyntax, string assemblyName, string assemblyId)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return null;
            }

            var className = classDeclarationSyntax.Identifier.ValueText;
            var expectedName = $"{assemblyId}HandlerRegistryExtensions";
            if (!StringComparer.Ordinal.Equals(className, expectedName))
            {
                return null;
            }

            if (!classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                !classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return null;
            }

            if (classDeclarationSyntax.Parent is TypeDeclarationSyntax)
            {
                return null;
            }

            if (classDeclarationSyntax.Parent is not BaseNamespaceDeclarationSyntax and not CompilationUnitSyntax)
            {
                return null;
            }

            var namespaceName = GetNamespace(classDeclarationSyntax);
            var visibility = classDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword) ? "public" : "internal";
            return new RootTypeSpec(namespaceName, visibility);
        }

        public static bool IsRootTypeCandidate(SyntaxNode node)
            => node is ClassDeclarationSyntax classDeclarationSyntax
               && classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
               && classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword)
               && classDeclarationSyntax.Identifier.ValueText.EndsWith("HandlerRegistryExtensions", StringComparison.Ordinal);

        static string GetNamespace(ImmutableArray<SymbolDisplayPart> handlerType) => handlerType.Length == 0 ? string.Empty : string.Join(".", handlerType.Where(x => x.Kind == SymbolDisplayPartKind.NamespaceName));

        static string GetNamespace(SyntaxNode syntaxNode)
        {
            Stack<string>? namespaces = null;
            for (SyntaxNode? current = syntaxNode.Parent; current is not null; current = current.Parent)
            {
                if (current is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
                {
                    continue;
                }

                namespaces ??= new Stack<string>();
                namespaces.Push(namespaceDeclaration.Name.ToString());
            }

            return namespaces is null ? string.Empty : string.Join(".", namespaces);
        }

        public static RootTypeSpec SelectRootTypeSpec(ImmutableArray<RootTypeSpec> specs)
        {
            if (specs.Length == 0)
            {
                return RootTypeSpec.Default;
            }

            var selected = specs[0];
            for (int i = 1; i < specs.Length; i++)
            {
                var candidate = specs[i];
                if (StringComparer.Ordinal.Compare(candidate.Namespace, selected.Namespace) < 0)
                {
                    selected = candidate;
                }
            }

            return selected;
        }

    }
}