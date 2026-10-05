namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Analyzer;
using Analyzer.Sagas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class GeneratedCorrelationAccessorExecutionTests
{
    [Test]
    public void Generated_correlation_accessors_round_trip_for_colliding_saga_data_properties()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.CollidingAccessorsAssembly.AddAll();
                         }
                     }

                     namespace First
                     {
                         [Saga]
                         public class SagaA : Saga<SagaAData>, IAmStartedByMessages<StartMessageA>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaAData> mapper) =>
                                 mapper.MapSaga(s => s.CorrelationId).ToMessage<StartMessageA>(m => m.CorrelationId);

                             public Task Handle(StartMessageA message, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         public class SagaAData : ContainSagaData
                         {
                             public string CorrelationId { get; set; }
                         }

                         public class StartMessageA : ICommand
                         {
                             public string CorrelationId { get; set; }
                         }
                     }

                     namespace Second
                     {
                         [Saga]
                         public class SagaB : Saga<SagaBData>, IAmStartedByMessages<StartMessageB>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaBData> mapper) =>
                                 mapper.MapSaga(s => s.CorrelationId).ToMessage<StartMessageB>(m => m.CorrelationId);

                             public Task Handle(StartMessageB message, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         public class SagaBData : ContainSagaData
                         {
                             public string CorrelationId { get; set; }
                         }

                         public class StartMessageB : ICommand
                         {
                             public string CorrelationId { get; set; }
                         }
                     }
                     """;

        var assembly = CompileAndLoad(source);

        // Two saga-data classes with a colliding property name/type must not share one generated accessor.
        var accessorTypes = assembly.GetTypes()
            .Where(t => typeof(CorrelationPropertyAccessor).IsAssignableFrom(t) && !t.IsAbstract)
            .ToArray();

        Assert.That(accessorTypes, Has.Length.EqualTo(2), "Each saga-data class must get its own generated correlation accessor.");

        foreach (var accessorType in accessorTypes)
        {
            var sagaDataType = accessorType
                .GetMethod("AccessFrom_Property", BindingFlags.Static | BindingFlags.NonPublic)!
                .GetParameters()[0]
                .ParameterType;
            var sagaData = (IContainSagaData)Activator.CreateInstance(sagaDataType);
            var accessor = (CorrelationPropertyAccessor)accessorType.GetField("Instance")!.GetValue(null)!;

            accessor.WriteTo(sagaData, "correlation-value");
            var value = accessor.AccessFrom(sagaData);

            Assert.That(value, Is.EqualTo("correlation-value"), $"Accessor for {sagaDataType.Name} did not round-trip the correlation value.");
        }
    }

    static Assembly CompileAndLoad(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var sourceTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            "CollidingAccessors",
            [sourceTree],
            ReferenceAssemblyPaths(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            [
                new AddSagaGenerator().AsSourceGenerator(),
                new AddHandlerAndSagasRegistrationGenerator().AsSourceGenerator()
            ],
            parseOptions: parseOptions);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        using var peStream = new MemoryStream();
        var emitResult = outputCompilation.Emit(peStream);

        var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));

        return Assembly.Load(peStream.ToArray());
    }

    static MetadataReference[] ReferenceAssemblyPaths() =>
    [
        .. AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrWhiteSpace(a.Location))
            .Select(MetadataReference (a) => MetadataReference.CreateFromFile(a.Location))
    ];
}
