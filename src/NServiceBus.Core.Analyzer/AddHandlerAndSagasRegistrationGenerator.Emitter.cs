#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Utility;
using static AddHandlerAndSagasRegistrationGenerator.Parser;

public partial class AddHandlerAndSagasRegistrationGenerator
{
    internal class Emitter(SourceProductionContext context)
    {
        public void Emit(ImmutableArray<BaseSpec> spec)
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

        public static NamespaceTree BuildNamespaceTree(IReadOnlyList<BaseSpec> baseSpecs)
        {
            var assemblyName = baseSpecs[0].AssemblyName;
            var assemblyId = SanitizeIdentifier(assemblyName);
            var root = BuildNamespaceNodeTree(baseSpecs, assemblyId);
            return new NamespaceTree(root, assemblyId, assemblyName);
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableArray<BaseSpec> baseSpecs)
        {
            Debug.Assert(baseSpecs.Length > 0);

            var namespaceTree = BuildNamespaceTree(baseSpecs);

            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// Provides extensions to register message handlers and sagas found in the <i>{namespaceTree.AssemblyName}</i> assembly.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WithGeneratedCodeAttribute();
            sourceWriter.WriteLine($"public static partial class {namespaceTree.AssemblyId}HandlerRegistryExtensions");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            EmitExtensionProperties(sourceWriter, namespaceTree.AssemblyId, namespaceTree.AssemblyName, namespaceTree.Root.RegistryName);

            sourceWriter.WriteLine();
            EmitNamespaceRegistry(sourceWriter, namespaceTree.Root);

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

        static void EmitNamespaceRegistry(SourceWriter sourceWriter, NamespaceNode node)
        {
            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// The registry for the <i>{node.RegistryName.Replace("RootRegistry", string.Empty)}</i> part.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WriteLine($"public sealed partial class {node.RegistryName}(global::NServiceBus.EndpointConfiguration configuration)");
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
            sourceWriter.WriteLine("""
                                   public void AddAll()
                                   {
                                       AddAllHandlers();
                                       AddAllSagas();
                                   }
                                   """);
            sourceWriter.WriteLine();

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
            sourceWriter.WriteLine();

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
                EmitNamespaceRegistry(sourceWriter, child);
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static NamespaceNode BuildNamespaceNodeTree(IReadOnlyList<BaseSpec> handlers, string assemblyId)
        {
            var root = new NamespaceNode($"{assemblyId}Root");

            foreach (var handler in handlers.OrderBy(spec => spec.Namespace, StringComparer.Ordinal)
                         .ThenBy(spec => spec.FullyQualifiedName, StringComparer.Ordinal))
            {
                var current = root;
                foreach (var part in GetNamespaceParts(handler.Namespace))
                {
                    current = current.GetOrAddChild(part);
                }

                current.Specs.Add(handler);
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

        internal record NamespaceTree(NamespaceNode Root, string AssemblyId, string AssemblyName);

        internal sealed class NamespaceNode(string name)
        {
            public string Name { get; } = name;

            public List<NamespaceNode> Children { get; } = [];

            public List<BaseSpec> Specs { get; } = [];

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
                Specs.Sort((left, right) => StringComparer.Ordinal.Compare(left.FullyQualifiedName, right.FullyQualifiedName));

                foreach (var child in Children)
                {
                    child.Sort();
                }
            }
        }
    }
}