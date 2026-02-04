#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Utility;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;
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

            var sourceWriter = new SourceWriter()
                .PreAmble()
                .WithOpenNamespace(rootTypeSpec.Namespace);

            EmitHandlers(sourceWriter, handlers, rootTypeSpec);
            sourceWriter.CloseCurlies();

            context.AddSource("HandlerRegistrations.Handlers.g.cs", sourceWriter.ToSourceText());
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableEquatableArray<HandlerSpec> handlers, BaseParser.RootTypeSpec rootTypeSpec)
        {
            Debug.Assert(handlers.Count > 0);

            var namespaceTree = BaseEmitter.BuildNamespaceTree(handlers, rootTypeSpec);

            sourceWriter.WriteLine($"{namespaceTree.RootTypeSpec.Visibility} static partial class {namespaceTree.ExtensionTypeName}");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            BaseEmitter.EmitNamespaceRegistry(
                sourceWriter,
                namespaceTree.Root,
                static (writer, current) =>
                {
                    writer.WriteLine($"{current.RootTypeSpec.Visibility} sealed partial class {current.RegistryName}");
                    writer.WriteLine("{");
                },
                 static (writer, current) =>
                {
                    if (current.Specs.Count == 0)
                    {
                        return;
                    }

                    writer.WriteLine("partial void AddAllHandlersCore()");
                    writer.WriteLine("{");
                    writer.Indentation++;

                    for (int index = 0; index < current.Specs.Count; index++)
                    {
                        var methodName = BaseEmitter.GetHandlerMethodName(current.Specs[index].Name, current.RootTypeSpec.RegistrationMethodNamePatterns);
                        writer.WriteLine($"{methodName}();");
                    }

                    writer.Indentation--;
                    writer.WriteLine("}");

                    writer.WriteLine();
                    BaseParser.RootTypeSpec rootTypeSpec = current.RootTypeSpec;
                    EmitHandlerMethods(writer, [.. current.Specs.Cast<HandlerSpec>()], rootTypeSpec.RegistrationMethodNamePatterns);
                });

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitHandlerMethods(SourceWriter sourceWriter, HandlerSpec[] handlerSpecs, ImmutableEquatableArray<string> registrationMethodNamePatterns)
        {
            for (int index = 0; index < handlerSpecs.Length; index++)
            {
                var handlerSpec = handlerSpecs[index];
                var methodName = BaseEmitter.GetHandlerMethodName(handlerSpec.Name, registrationMethodNamePatterns);
                sourceWriter.WriteLine("/// <summary>");
                sourceWriter.WriteLine($"""/// Registers the <see cref="{handlerSpec.FullyQualifiedName}"/> handler with the endpoint configuration.""");
                sourceWriter.WriteLine("/// </summary>");
                sourceWriter.WriteLine($"public void {methodName}()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;

                Handlers.Emitter.EmitHandlerRegistryVariables(sourceWriter, "_configuration");
                Handlers.Emitter.EmitHandlerRegistryCode(sourceWriter, handlerSpec);

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");

                if (index < handlerSpecs.Length - 1)
                {
                    sourceWriter.WriteLine();
                }
            }
        }
    }
}