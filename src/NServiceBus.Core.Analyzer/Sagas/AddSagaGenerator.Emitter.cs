namespace NServiceBus.Core.Analyzer.Sagas;

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Utility;
using static Sagas;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;
using BaseEmitter = AddHandlerAndSagasRegistrationGenerator.Emitter;

public partial class AddSagaGenerator
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(SagaSpecs sagaSpecs, BaseParser.RootTypeSpec rootTypeSpec) => Emit(sourceProductionContext, sagaSpecs, rootTypeSpec);

        static void Emit(SourceProductionContext context, SagaSpecs sagaSpecs, BaseParser.RootTypeSpec rootTypeSpec)
        {
            var sagas = sagaSpecs.Sagas;
            if (sagas.Count == 0)
            {
                return;
            }
            var sourceWriter = new SourceWriter()
                .PreAmble()
                .WithOpenNamespace(rootTypeSpec.Namespace);

            EmitHandlers(sourceWriter, sagas, rootTypeSpec);
            sourceWriter.CloseCurlies();

            context.AddSource("HandlerRegistrations.Sagas.g.cs", sourceWriter.ToSourceText());
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableEquatableArray<SagaSpec> sagas, BaseParser.RootTypeSpec rootTypeSpec)
        {
            Debug.Assert(sagas.Count > 0);

            var namespaceTree = BaseEmitter.BuildNamespaceTree(sagas, rootTypeSpec);

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

                    writer.WriteLine("partial void AddAllSagasCore()");
                    writer.WriteLine("{");
                    writer.Indentation++;

                    for (int index = 0; index < current.Specs.Count; index++)
                    {
                        var methodName = BaseEmitter.GetSagaMethodName(current.Specs[index].Name, current.RootTypeSpec.RegistrationMethodNamePatterns);
                        writer.WriteLine($"{methodName}();");
                    }

                    writer.Indentation--;
                    writer.WriteLine("}");

                    writer.WriteLine();
                    EmitHandlerMethods(writer, [.. current.Specs.Cast<SagaSpec>()], current.RootTypeSpec);
                });

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            Sagas.Emitter.EmitAccessors(sourceWriter, sagas);
        }

        static void EmitHandlerMethods(SourceWriter sourceWriter, SagaSpec[] sagaSpecs, BaseParser.RootTypeSpec rootTypeSpec)
        {
            for (int index = 0; index < sagaSpecs.Length; index++)
            {
                var sagaSpec = sagaSpecs[index];
                var methodName = BaseEmitter.GetSagaMethodName(sagaSpec.Name, rootTypeSpec.RegistrationMethodNamePatterns);
                sourceWriter.WriteLine("/// <summary>");
                sourceWriter.WriteLine($"""/// Registers the <see cref="{sagaSpec.FullyQualifiedName}"/> saga with the endpoint configuration.""");
                sourceWriter.WriteLine("/// </summary>");
                sourceWriter.WriteLine($"public void {methodName}()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;

                Sagas.Emitter.EmitSagaRegistrationBlock(sourceWriter, sagaSpec, "_configuration");

                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");

                if (index < sagaSpecs.Length - 1)
                {
                    sourceWriter.WriteLine();
                }
            }
        }
    }
}