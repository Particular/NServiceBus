#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Utility;
using BaseParser = NServiceBus.Core.Analyzer.AddHandlerAndSagasRegistrationGenerator.Parser;
using static Handlers;
using BaseEmitter = AddHandlerAndSagasRegistrationGenerator.Emitter;

public sealed partial class AddHandlerGenerator
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(HandlerSpecs handlerSpecs, BaseParser.RootTypeSpec rootTypeSpec) => Emit(sourceProductionContext, handlerSpecs, rootTypeSpec);

        static void Emit(SourceProductionContext context, HandlerSpecs handlerSpecs, BaseParser.RootTypeSpec rootTypeSpec)
        {
            var handlers = handlerSpecs.Handlers;
            if (handlers.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter();
            OpenNamespace(sourceWriter, rootTypeSpec.Namespace);

            EmitHandlers(sourceWriter, handlers, rootTypeSpec);
            sourceWriter.CloseCurlies();

            context.AddSource("HandlerRegistrations.g.cs", sourceWriter.ToSourceText());
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableEquatableArray<HandlerSpec> handlers, BaseParser.RootTypeSpec rootTypeSpec)
        {
            Debug.Assert(handlers.Count > 0);

            var namespaceTree = BaseEmitter.BuildNamespaceTree(handlers, rootTypeSpec);

            sourceWriter.WriteLine($"{namespaceTree.Visibility} static partial class {namespaceTree.AssemblyId}HandlerRegistryExtensions");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            EmitNamespaceRegistry(sourceWriter, namespaceTree.Root, namespaceTree.Visibility);

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitNamespaceRegistry(SourceWriter sourceWriter, BaseEmitter.NamespaceNode node, string typeVisibility)
        {
            sourceWriter.WriteLine($"{typeVisibility} sealed partial class {node.RegistryName}");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            if (node.Specs.Count > 0)
            {
                sourceWriter.WriteLine("partial void AddAllHandlersCore()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;

                for (int index = 0; index < node.Specs.Count; index++)
                {
                    var methodName = GetSingleHandlerMethodName(node.Specs[index].Name);
                    sourceWriter.WriteLine($"{methodName}();");
                }

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");

                sourceWriter.WriteLine();
                EmitHandlerMethods(sourceWriter, [.. node.Specs.Cast<HandlerSpec>()]);
            }

            foreach (var child in node.Children)
            {
                EmitNamespaceRegistry(sourceWriter, child, typeVisibility);
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void OpenNamespace(SourceWriter sourceWriter, string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return;
            }

            sourceWriter.WriteLine($"namespace {namespaceName}");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;
        }

        static void EmitHandlerMethods(SourceWriter sourceWriter, HandlerSpec[] handlerSpecs)
        {
            for (int index = 0; index < handlerSpecs.Length; index++)
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

                if (index < handlerSpecs.Length - 1)
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
    }
}
