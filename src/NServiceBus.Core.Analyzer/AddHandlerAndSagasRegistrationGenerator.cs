#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Utility;

[Generator(LanguageNames.CSharp)]
public partial class AddHandlerAndSagasRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: Parser.Parse)
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!.Value)
            .WithTrackingName("HandlerSpec");

        var addSagas = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.SagaAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: Parser.Parse)
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!.Value)
            .WithTrackingName("SagaSpec");

        var collected = addHandlers.Collect()
            .Combine(addSagas.Collect())
            .Select((pair, _) => pair.Left.AddRange(pair.Right))
            .WithTrackingName("HandlerAndSagaSpecs");

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }
}

public partial class AddHandlerAndSagasRegistrationGenerator
{
    internal static class Parser
    {
        internal readonly record struct HandlerOrSagaBaseSpec(string Namespace, string AssemblyName, string Type);

        public static HandlerOrSagaBaseSpec? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default) => context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol ? null : Parse(namedTypeSymbol);

        static HandlerOrSagaBaseSpec Parse(INamedTypeSymbol namedTypeSymbol)
        {
            var fullyQualifiedName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var displayParts = namedTypeSymbol.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat);
            var handlerOrSagaNamespace = GetNamespace(displayParts);
            var assemblyName = namedTypeSymbol.ContainingAssembly?.Name ?? "Assembly";
            return new HandlerOrSagaBaseSpec(Namespace: handlerOrSagaNamespace, AssemblyName: assemblyName, Type: fullyQualifiedName);
        }

        static string GetNamespace(ImmutableArray<SymbolDisplayPart> handlerType) => handlerType.Length == 0 ? string.Empty : string.Join(".", handlerType.Where(x => x.Kind == SymbolDisplayPartKind.NamespaceName));
    }
}

public partial class AddHandlerAndSagasRegistrationGenerator
{
    internal class Emitter(SourceProductionContext context)
    {
        public void Emit(ImmutableArray<Parser.HandlerOrSagaBaseSpec> spec)
        {
            if (spec.Length == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter();
            sourceWriter.WriteLine("""
                                   namespace NServiceBus
                                   {
                                   """);
            sourceWriter.Indentation++;

            EmitHandlers(sourceWriter, spec);
            sourceWriter.CloseCurlies();

            context.AddSource("Registrations.g.cs", sourceWriter.ToSourceText());
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableArray<Parser.HandlerOrSagaBaseSpec> handlersOrSagas)
        {
            Debug.Assert(handlersOrSagas.Length > 0);

            var root = BuildNamespaceTree(handlersOrSagas);
            var assemblyName = handlersOrSagas[0].AssemblyName;
            var assemblyId = SanitizeIdentifier(assemblyName);

            var rootRegistryName = $"{assemblyId}RootRegistry";

            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// Provides extensions to register message handlers and sagas found in the <i>{assemblyName}</i> assembly.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WithGeneratedCodeAttribute();
            sourceWriter.WriteLine($"public static partial class {assemblyId}HandlerRegistryExtensions");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            EmitExtensionProperties(sourceWriter, assemblyId, assemblyName, rootRegistryName);

            sourceWriter.WriteLine();
            EmitNamespaceRegistry(sourceWriter, root, rootRegistryName);

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitExtensionProperties(SourceWriter sourceWriter, string assemblyId, string assemblyName, string rootRegistryName)
        {
            sourceWriter.WriteLine("extension (global::NServiceBus.HandlerRegistry registry)");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// Gets the registry for the <i>{assemblyName}</i> assembly.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WriteLine($"public {rootRegistryName} {assemblyId}Assembly => new(registry.Configuration);");

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitNamespaceRegistry(SourceWriter sourceWriter, NamespaceNode node, string registryName)
        {
            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// The registry for the <i>{node.Name ?? registryName.Replace("RootRegistry", string.Empty)}</i> part.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WriteLine($"public sealed partial class {registryName}(global::NServiceBus.EndpointConfiguration configuration)");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            sourceWriter.WriteLine("readonly global::NServiceBus.EndpointConfiguration _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));");

            if (node.Children.Count > 0)
            {
                sourceWriter.WriteLine();
            }

            foreach (var child in node.Children)
            {
                sourceWriter.WriteLine("/// <summary>");
                sourceWriter.WriteLine($"/// Gets the registry for the <i>{child.Name}</i> namespace part.");
                sourceWriter.WriteLine("/// </summary>");
                sourceWriter.WriteLine($"public {child.RegistryName} {child.Name} => new(_configuration);");
                sourceWriter.WriteLine();
            }

            if (node.Children.Count == 0)
            {
                sourceWriter.WriteLine();
            }
            //
            //
            //
            // sourceWriter.WriteLine("/// <summary>");
            // if (node.Children.Count > 0)
            // {
            //     sourceWriter.WriteLine($"/// Adds handler or saga registrations from the {string.Join(", ", node.Children.Select(c => $"<i>{c.Name}</i>"))} parts including all the subnamespaces.");
            // }
            //
            // if (node.HandlersOrSagas.Count > 0)
            // {
            //     sourceWriter.WriteLine($"/// Adds {string.Join(", ", node.HandlersOrSagas.Select(c => $"<see cref=\"{c.Type}\"/>"))} handlers and sagas from <i>{node.Name}<i/> namespace.");
            // }
            // sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WriteLine("""
                                   public void AddAll()
                                   {
                                       AddAllHandlers();
                                       AddAllSagas();
                                   }
                                   """);

            sourceWriter.WriteLine("public void AddAllHandlers()");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;
            sourceWriter.WriteLine("AddAllHandlersCore();");

            foreach (var child in node.Children)
            {
                sourceWriter.WriteLine($"{child.Name}.AddAllHandlers();");
            }
            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            sourceWriter.WriteLine("public void AddAllSagas()");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;
            sourceWriter.WriteLine("AddAllSagasCore();");

            foreach (var child in node.Children)
            {
                sourceWriter.WriteLine($"{child.Name}.AddAllSagas();");
            }
            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            sourceWriter.WriteLine();
            sourceWriter.WriteLine("partial void AddAllHandlersCore();");
            sourceWriter.WriteLine("partial void AddAllSagasCore();");

            foreach (var child in node.Children)
            {
                sourceWriter.WriteLine();
                EmitNamespaceRegistry(sourceWriter, child, child.RegistryName);
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static NamespaceNode BuildNamespaceTree(ImmutableArray<Parser.HandlerOrSagaBaseSpec> handlers)
        {
            var root = new NamespaceNode(null);

            foreach (var handler in handlers.OrderBy(spec => spec.Namespace, StringComparer.Ordinal)
                         .ThenBy(spec => spec.Type, StringComparer.Ordinal))
            {
                var current = root;
                foreach (var part in GetNamespaceParts(handler.Namespace))
                {
                    current = current.GetOrAddChild(part);
                }

                current.HandlersOrSagas.Add(handler);
            }

            root.Sort();
            return root;
        }

        static string[] GetNamespaceParts(string handlerNamespace)
        {
            var parts = handlerNamespace.Split(['.'], StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && string.Equals(parts[0], "NServiceBus", StringComparison.Ordinal))
            {
                return parts[1..];
            }

            return parts;
        }

        static string SanitizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Assembly";
            }

            var sanitized = System.Text.RegularExpressions.Regex.Replace(value, @"[^a-zA-Z0-9]", "");

            return char.IsDigit(sanitized[0]) ? $"_{sanitized}" : sanitized;
        }

        sealed class NamespaceNode(string? name)
        {
            public string? Name { get; } = name;

            public List<NamespaceNode> Children { get; } = [];

            public List<Parser.HandlerOrSagaBaseSpec> HandlersOrSagas { get; } = [];

            public string RegistryName => $"{Name}Registry";

            public NamespaceNode GetOrAddChild(string childName)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    if (StringComparer.Ordinal.Equals(child.Name, childName))
                    {
                        return child;
                    }
                }

                var newChild = new NamespaceNode(childName);
                Children.Add(newChild);
                return newChild;
            }

            public void Sort()
            {
                Children.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
                HandlersOrSagas.Sort((left, right) => StringComparer.Ordinal.Compare(left.Type, right.Type));

                foreach (var child in Children)
                {
                    child.Sort();
                }
            }
        }
    }
}



