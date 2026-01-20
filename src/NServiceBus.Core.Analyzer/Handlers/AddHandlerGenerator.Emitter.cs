#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Utility;

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

            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// Provides extensions to register message handlers found in the <i>{assemblyName}</i> assembly.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WithGeneratedCodeAttribute();
            sourceWriter.WriteLine($"public static class {assemblyId}HandlerRegistryExtensions");
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
            sourceWriter.WriteLine($"/// Gets the handler registry for the <i>{assemblyName}</i> assembly.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WriteLine($"public {rootRegistryName} {assemblyId}Assembly => new(registry.Configuration);");

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitNamespaceRegistry(SourceWriter sourceWriter, NamespaceNode node, string registryName)
        {
            sourceWriter.WriteLine("/// <summary>");
            sourceWriter.WriteLine($"/// The handler registry for the <i>{node.Name ?? registryName.Replace("RootRegistry", string.Empty)}</i> part.");
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WriteLine($"public sealed class {registryName}(global::NServiceBus.EndpointConfiguration configuration)");
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
                sourceWriter.WriteLine($"/// Gets the handler registry for the <i>{child.Name}</i> namespace part.");
                sourceWriter.WriteLine("/// </summary>");
                sourceWriter.WriteLine($"public {child.RegistryName} {child.Name} => new(_configuration);");
                sourceWriter.WriteLine();
            }

            if (node.Children.Count == 0)
            {
                sourceWriter.WriteLine();
            }
            sourceWriter.WriteLine("/// <summary>");
            if (node.Children.Count > 0)
            {
                sourceWriter.WriteLine($"/// Adds handler registrations from the {string.Join(", ", node.Children.Select(c => $"<i>{c.Name}</i>"))} parts including all the subnamespaces.");
            }

            if (node.Handlers.Count > 0)
            {
                sourceWriter.WriteLine($"/// Adds {string.Join(", ", node.Handlers.Select(c => $"<see cref=\"{c.HandlerType}\"/>"))} handlers from <i>{node.Name}<i/> namespace.");
            }
            sourceWriter.WriteLine("/// </summary>");
            sourceWriter.WriteLine("public void AddAll()");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            foreach (var child in node.Children)
            {
                sourceWriter.WriteLine($"{child.Name}.AddAll();");
            }

            if (node.Children.Count > 0 && node.Handlers.Count > 0)
            {
                sourceWriter.WriteLine();
            }

            for (int index = 0; index < node.Handlers.Count; index++)
            {
                var methodName = GetSingleHandlerMethodName(node.Handlers[index].Name);
                sourceWriter.WriteLine($"{methodName}();");
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            if (node.Handlers.Count > 0)
            {
                sourceWriter.WriteLine();
                EmitHandlerMethods(sourceWriter, node.Handlers);
            }

            foreach (var child in node.Children)
            {
                sourceWriter.WriteLine();
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
                sourceWriter.WriteLine($"""/// Adds the <see cref="{handlerSpec.HandlerType}"/> handler to the registration.""");
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
                EmitHandlerRegistryCode(sourceWriter, handlerSpec);
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

            foreach (var handler in handlers.OrderBy(spec => spec.HandlerNamespace, StringComparer.Ordinal)
                         .ThenBy(spec => spec.HandlerType, StringComparer.Ordinal))
            {
                var current = root;
                foreach (var part in GetNamespaceParts(handler.HandlerNamespace))
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

        public static void EmitHandlerRegistryCode(SourceWriter sourceWriter, HandlerSpec handlerSpec)
        {
            foreach (var registration in handlerSpec.Registrations)
            {
                var addType = registration.RegistrationType switch
                {
                    RegistrationType.MessageHandler or RegistrationType.StartMessageHandler => "Message",
                    RegistrationType.TimeoutHandler => "Timeout",
                    _ => "Message"
                };

                sourceWriter.WriteLine($"messageHandlerRegistry.Add{addType}HandlerForMessage<{registration.HandlerType}, {registration.MessageType}>();");
                var hierarchyLiteral = $"[{string.Join(", ", registration.MessageHierarchy.Select(type => $"typeof({type})"))}]";
                sourceWriter.WriteLine($"messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof({registration.MessageType}), {hierarchyLiteral});");
            }
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
                Handlers.Sort((left, right) => StringComparer.Ordinal.Compare(left.HandlerType, right.HandlerType));

                foreach (var child in Children)
                {
                    child.Sort();
                }
            }
        }
    }
}