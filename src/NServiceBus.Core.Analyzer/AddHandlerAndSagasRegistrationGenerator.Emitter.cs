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
        public void Emit(ImmutableArray<BaseSpec> spec, RootTypeSpec rootTypeSpec)
        {
            if (spec.Length == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter()
                .WithOpenNamespace(rootTypeSpec.Namespace);

            EmitHandlers(sourceWriter, spec, rootTypeSpec);
            sourceWriter.CloseCurlies();

            context.AddSource("Registrations.g.cs", sourceWriter.ToSourceText());
        }

        public static NamespaceTree BuildNamespaceTree(IReadOnlyList<BaseSpec> baseSpecs, RootTypeSpec rootTypeSpec)
        {
            var assemblyName = baseSpecs[0].AssemblyName;
            var rootName = rootTypeSpec.RootName;
            var root = BuildNamespaceNodeTree(baseSpecs, rootName);
            return new NamespaceTree(root, rootName, rootTypeSpec.ExtensionTypeName, assemblyName, rootTypeSpec.Namespace, rootTypeSpec.Visibility);
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableArray<BaseSpec> baseSpecs, RootTypeSpec rootTypeSpec)
        {
            Debug.Assert(baseSpecs.Length > 0);

            var namespaceTree = BuildNamespaceTree(baseSpecs, rootTypeSpec);

            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// Provides extensions to register message handlers and sagas found in the <i>{namespaceTree.AssemblyName}</i> assembly.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WithGeneratedCodeAttribute();
            sourceWriter.WriteLine($"{namespaceTree.Visibility} static partial class {namespaceTree.ExtensionTypeName}");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            EmitExtensionProperties(sourceWriter, namespaceTree.RootName, namespaceTree.AssemblyName, namespaceTree.Root.RegistryName);

            sourceWriter.WriteLine();
            EmitNamespaceRegistry(sourceWriter, namespaceTree.Root, namespaceTree.Visibility);

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitExtensionProperties(SourceWriter sourceWriter, string rootName, string assemblyName, string rootRegistryName)
        {
            sourceWriter.WriteLine("extension (global::NServiceBus.HandlerRegistry registry)");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// Gets the registry for the <i>{assemblyName}</i> assembly.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WriteLine($"public {rootRegistryName} {rootName}Assembly => new(registry.Configuration);");

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitNamespaceRegistry(SourceWriter sourceWriter, NamespaceNode node, string typeVisibility) =>
            EmitNamespaceRegistry(
                sourceWriter,
                node,
                typeVisibility,
                static (writer, current, visibility) =>
                {
                    writer.WriteLine("/// <summary>");
                    writer.WriteLine($"/// The registry for the <i>{(current.Name.EndsWith("Root") ? current.Name.TrimEnd('R', 'o', 'o', 't') : current.Name)}</i> part.");
                    writer.WriteLine("/// </summary>");
                    writer.WriteLine($"{visibility} sealed partial class {current.RegistryName}(global::NServiceBus.EndpointConfiguration configuration)");
                    writer.WriteLine("{");
                },
                static (writer, current, _) =>
                {
                    writer.WriteLine("readonly global::NServiceBus.EndpointConfiguration _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));");

                    if (current.Children.Count > 0)
                    {
                        writer.WriteLine();
                    }

                    foreach (var child in current.Children)
                    {
                        writer.WriteLine("/// <summary>");
                        writer.WriteLine($"/// Gets the registry for the <i>{child.Name}</i> namespace part.");
                        writer.WriteLine("/// </summary>");
                        writer.WriteLine($"public {child.RegistryName} {child.Name} => new(_configuration);");
                        writer.WriteLine();
                    }

                    if (current.Children.Count == 0)
                    {
                        writer.WriteLine();
                    }
                    writer.WriteLine("""
                                     public void AddAll()
                                     {
                                         AddAllHandlers();
                                         AddAllSagas();
                                     }
                                     """);
                    writer.WriteLine();

                    writer.WriteLine("public void AddAllHandlers()");
                    writer.WriteLine("{");
                    writer.Indentation++;
                    writer.WriteLine("AddAllHandlersCore();");

                    foreach (var child in current.Children)
                    {
                        writer.WriteLine($"{child.Name}.AddAllHandlers();");
                    }
                    writer.Indentation--;
                    writer.WriteLine("}");
                    writer.WriteLine();

                    writer.WriteLine("public void AddAllSagas()");
                    writer.WriteLine("{");
                    writer.Indentation++;
                    writer.WriteLine("AddAllSagasCore();");

                    foreach (var child in current.Children)
                    {
                        writer.WriteLine($"{child.Name}.AddAllSagas();");
                    }
                    writer.Indentation--;
                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("partial void AddAllHandlersCore();");
                    writer.WriteLine("partial void AddAllSagasCore();");
                },
                static (writer, _, _) => writer.WriteLine());

        internal static void EmitNamespaceRegistry(
            SourceWriter sourceWriter,
            NamespaceNode node,
            string typeVisibility,
            Action<SourceWriter, NamespaceNode, string> emitHeader,
            Action<SourceWriter, NamespaceNode, string> emitBody,
            Action<SourceWriter, NamespaceNode, NamespaceNode>? emitBeforeChild = null)
        {
            emitHeader(sourceWriter, node, typeVisibility);
            sourceWriter.Indentation++;

            emitBody(sourceWriter, node, typeVisibility);

            foreach (var child in node.Children)
            {
                emitBeforeChild?.Invoke(sourceWriter, node, child);
                EmitNamespaceRegistry(sourceWriter, child, typeVisibility, emitHeader, emitBody, emitBeforeChild);
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static NamespaceNode BuildNamespaceNodeTree(IReadOnlyList<BaseSpec> handlers, string rootName)
        {
            var root = new NamespaceNode($"{rootName}Root");

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

        internal static string SanitizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Assembly";
            }

            var sanitized = System.Text.RegularExpressions.Regex.Replace(value, @"[^a-zA-Z0-9]", "");

            return char.IsDigit(sanitized[0]) ? $"_{sanitized}" : sanitized;
        }

        internal record NamespaceTree(NamespaceNode Root, string RootName, string ExtensionTypeName, string AssemblyName, string Namespace, string Visibility);

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