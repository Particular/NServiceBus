#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
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
            var interfaceLessHandlers = handlers
                .SelectMany(handler => handler.InterfaceLessHandlers)
                .OrderBy(spec => spec.GeneratedHandlerType, StringComparer.Ordinal)
                .ToArray();

            for (int index = 0; index < interfaceLessHandlers.Length; index++)
            {
                var handler = interfaceLessHandlers[index];
                EmitInterfaceLessHandler(sourceWriter, handler);

                if (index < interfaceLessHandlers.Length - 1)
                {
                    sourceWriter.WriteLine();
                }
            }

            if (interfaceLessHandlers.Length > 0)
            {
                sourceWriter.WriteLine();
            }

            sourceWriter.WriteLine("""
                                   namespace NServiceBus
                                   {
                                   """);
            sourceWriter.Indentation++;

            EmitHandlers(sourceWriter, handlers);
            sourceWriter.CloseCurlies();

            context.AddSource("Handlers.g.cs", sourceWriter.ToSourceText());
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableEquatableArray<HandlerSpec> handlers)
        {
            var root = BuildNamespaceTree(handlers);
            var assemblyId = GetAssemblyIdentifier(handlers);
            var rootRegistryName = $"{assemblyId}RootRegistry";

            sourceWriter.WithGeneratedCodeAttribute();
            sourceWriter.WriteLine($"public static class {assemblyId}HandlerRegistryExtensions");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            EmitExtensionProperties(sourceWriter, assemblyId, rootRegistryName);

            sourceWriter.WriteLine();
            EmitNamespaceRegistry(sourceWriter, root, rootRegistryName);

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitExtensionProperties(SourceWriter sourceWriter, string assemblyId, string rootRegistryName)
        {
            sourceWriter.WriteLine("extension (global::NServiceBus.HandlerRegistry registry)");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            sourceWriter.WriteLine($"public {rootRegistryName} {assemblyId}Assembly => new(registry.Configuration);");

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitNamespaceRegistry(SourceWriter sourceWriter, NamespaceNode node, string registryName)
        {
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
                sourceWriter.WriteLine($"public {child.RegistryName} {child.Name} => new(_configuration);");
            }

            sourceWriter.WriteLine();
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

            if (handlerName.EndsWith(HandlerSuffix, StringComparison.Ordinal))
            {
                handlerName = handlerName[..^HandlerSuffix.Length];
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

        static string GetAssemblyIdentifier(ImmutableEquatableArray<HandlerSpec> handlers)
        {
            var assemblyName = handlers.Count > 0 ? handlers[0].AssemblyName : "Assembly";
            var sanitizedAssemblyName = SanitizeIdentifier(assemblyName);
            return sanitizedAssemblyName;
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

        static void EmitInterfaceLessHandler(SourceWriter sourceWriter, InterfaceLessHandlerSpec handlerSpec)
        {
            sourceWriter.WithGeneratedCodeAttribute();
            sourceWriter.WriteLine($$"""
                                     [System.Diagnostics.DebuggerNonUserCode]
                                     file sealed class {{handlerSpec.GeneratedHandlerName}} : NServiceBus.IHandleMessages<{{handlerSpec.MessageType}}>
                                     {
                                     """);

            sourceWriter.Indentation++;

            foreach (var parameter in handlerSpec.ConstructorParameters)
            {
                sourceWriter.WriteLine($"readonly {parameter.Type} {parameter.FieldName};");
            }

            sourceWriter.WriteLine($"readonly {handlerSpec.DeclaringHandlerType} _instance;");
            sourceWriter.WriteLine($"readonly System.IServiceProvider _serviceProvider;");

            sourceWriter.WriteLine();

            var constructorParameters = string.Join(", ", handlerSpec.ConstructorParameters.Select(p => $"{p.Type} {p.Name}"));
            sourceWriter.WriteLine(handlerSpec.ConstructorParameters.Count > 0 ? $"public {handlerSpec.GeneratedHandlerName}({constructorParameters}, System.IServiceProvider serviceProvider)" : $"public {handlerSpec.GeneratedHandlerName}(System.IServiceProvider serviceProvider)");
            sourceWriter.WriteLine("{");

            sourceWriter.Indentation++;

            foreach (var parameter in handlerSpec.ConstructorParameters)
            {
                sourceWriter.WriteLine($"{parameter.FieldName} = {parameter.Name};");
            }

            sourceWriter.WriteLine("_serviceProvider = serviceProvider ?? throw new System.ArgumentNullException(nameof(serviceProvider));");
            sourceWriter.WriteLine($"_instance = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<{handlerSpec.DeclaringHandlerType}>(_serviceProvider, []);");

            sourceWriter.Indentation--;

            sourceWriter.WriteLine("}");
            sourceWriter.WriteLine();

            sourceWriter.WriteLine($"public System.Threading.Tasks.Task Handle({handlerSpec.MessageType} message, NServiceBus.IMessageHandlerContext context)");
            sourceWriter.WriteLine("{");

            sourceWriter.Indentation++;

            var arguments = handlerSpec.ConstructorParameters.Count == 0
                ? "message, context"
                : $"message, context, {string.Join(", ", handlerSpec.ConstructorParameters.Select(p => p.FieldName))}";
            sourceWriter.WriteLine(!handlerSpec.IsStatic ? $"return _instance.Handle({arguments});" : $"return {handlerSpec.DeclaringHandlerType}.Handle({arguments});");

            sourceWriter.Indentation--;

            sourceWriter.WriteLine("}");

            sourceWriter.Indentation--;

            sourceWriter.WriteLine("}");
        }
    }
}