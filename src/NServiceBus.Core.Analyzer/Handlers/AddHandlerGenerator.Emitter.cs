#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Utility;
using static Handlers;

public sealed partial class AddHandlerGenerator
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(HandlerSpecs handlerSpecs) => Emit(sourceProductionContext, handlerSpecs);

        static void Emit(SourceProductionContext context, HandlerSpecs handlerSpecs)
        {
            var handlers = handlerSpecs.Handlers;
            if (handlers.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter();
            sourceWriter.WriteLine("""
                                   namespace NServiceBus
                                   {
                                   """);
            sourceWriter.Indentation++;

            EmitHandlers(sourceWriter, handlers);
            sourceWriter.CloseCurlies();

            context.AddSource("HandlerRegistrations.g.cs", sourceWriter.ToSourceText());
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableEquatableArray<HandlerSpec> handlers)
        {
            Debug.Assert(handlers.Count > 0);

            var root = BuildNamespaceTree(handlers);
            var assemblyName = handlers[0].AssemblyName;
            var assemblyId = SanitizeIdentifier(assemblyName);

            var rootRegistryName = $"{assemblyId}RootRegistry";

            sourceWriter.WriteLine($"public static partial class {assemblyId}HandlerRegistryExtensions");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            EmitNamespaceRegistry(sourceWriter, root, rootRegistryName);

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitNamespaceRegistry(SourceWriter sourceWriter, NamespaceNode node, string registryName)
        {
            sourceWriter.WriteLine($"public sealed partial class {registryName}");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            if (node.Handlers.Count > 0)
            {
                sourceWriter.WriteLine("partial void AddAllHandlersCore()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;

                for (int index = 0; index < node.Handlers.Count; index++)
                {
                    var methodName = GetSingleHandlerMethodName(node.Handlers[index].Name);
                    sourceWriter.WriteLine($"{methodName}();");
                }

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");

                sourceWriter.WriteLine();
                EmitHandlerMethods(sourceWriter, node.Handlers);
            }

            foreach (var child in node.Children)
            {
                EmitNamespaceRegistry(sourceWriter, child, child.RegistryName);
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitHandlerMethods(SourceWriter sourceWriter, List<HandlerSpec> handlerSpecs)
        {
            for (int index = 0; index < handlerSpecs.Count; index++)
            {
                var handlerSpec = handlerSpecs[index];
                var methodName = GetSingleHandlerMethodName(handlerSpec.Name);
                sourceWriter.WriteLine("/// <summary>");
                sourceWriter.WriteLine($"""/// Adds the <see cref="{handlerSpec.FullyQualifiedName}"/> handler to the registration.""");
                sourceWriter.WriteLine("/// </summary>");
                sourceWriter.WriteLine($"public void {methodName}()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;

                EmitHandlerRegistrationBlock(sourceWriter, [handlerSpec], "_configuration");

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");

                if (index < handlerSpecs.Count - 1)
                {
                    sourceWriter.WriteLine();
                }
            }
        }

        static void EmitHandlerRegistrationBlock(SourceWriter sourceWriter, IReadOnlyList<HandlerSpec> handlerSpecs, string configurationVariable)
        {
            sourceWriter.WriteLine($"""
                                    var settings = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings({configurationVariable});
                                    var messageHandlerRegistry = settings.GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                                    var messageMetadataRegistry = settings.GetOrCreate<NServiceBus.Unicast.Messages.MessageMetadataRegistry>();
                                    """);

            foreach (var handlerSpec in handlerSpecs)
            {
                Handlers.Emitter.EmitHandlerRegistryCode(sourceWriter, handlerSpec);
            }
        }

        static string GetSingleHandlerMethodName(string handlerName)
        {
            const string HandlerSuffix = "Handler";

            if (!handlerName.EndsWith(HandlerSuffix, StringComparison.Ordinal))
            {
                handlerName += HandlerSuffix;
            }

            return $"Add{handlerName}";
        }

        static NamespaceNode BuildNamespaceTree(ImmutableEquatableArray<HandlerSpec> handlers)
        {
            var root = new NamespaceNode(null);

            foreach (var handler in handlers.OrderBy(spec => spec.Namespace, StringComparer.Ordinal)
                         .ThenBy(spec => spec.FullyQualifiedName, StringComparer.Ordinal))
            {
                var current = root;
                foreach (var part in GetNamespaceParts(handler.Namespace))
                {
                    current = current.GetOrAddChild(part);
                }

                current.Handlers.Add(handler);
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

            public List<HandlerSpec> Handlers { get; } = [];

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
                Handlers.Sort((left, right) => StringComparer.Ordinal.Compare(left.FullyQualifiedName, right.FullyQualifiedName));

                foreach (var child in Children)
                {
                    child.Sort();
                }
            }
        }
    }
}