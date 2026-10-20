namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Analyzer.Sagas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class SagaTimeoutManagementPersistenceAnalyzerTests : AnalyzerTestFixture<SagaTimeoutManagementPersistenceAnalyzer>
{
    [Test]
    public Task ReportsReportingWithUnsupportedPersistence()
    {
        var source = $$"""
                     using NServiceBus;

                     class Configure
                     {
                         void Endpoint(EndpointConfiguration config)
                         {
                             config.UsePersistence<NoTimeoutsPersistence>();
                             [|config.ReportSagaTimeoutsToServiceControl()|];
                         }
                     }

                     {{NoTimeoutsPersistence}}
                     """;

        return Assert(source, DiagnosticIds.SagaTimeoutReportingRequiresSupportingPersistence);
    }

    [Test]
    public Task ReportsReportingOnLocalConfiguration()
    {
        var source = $$"""
                     using NServiceBus;

                     class Configure
                     {
                         void Endpoint()
                         {
                             var config = new EndpointConfiguration("Sales");
                             [|config.ReportSagaTimeoutsToServiceControl()|];
                             var persistence = config.UsePersistence<NoTimeoutsPersistence, StorageType.Sagas>();
                         }
                     }

                     {{NoTimeoutsPersistence}}
                     """;

        return Assert(source, DiagnosticIds.SagaTimeoutReportingRequiresSupportingPersistence);
    }

    [Test]
    public Task DoesNotReportReportingWithSupportedPersistence()
    {
        var source = """
                     using NServiceBus;

                     class Configure
                     {
                         void Endpoint(EndpointConfiguration config)
                         {
                             config.UsePersistence<LearningPersistence>();
                             config.ReportSagaTimeoutsToServiceControl();
                         }
                     }
                     """;

        return Assert(source);
    }

    [Test]
    public Task UsesExplicitSagaStorageOverOtherPersistences()
    {
        var source = $$"""
                     using NServiceBus;

                     class Configure
                     {
                         void SagasOnLearning(EndpointConfiguration config)
                         {
                             config.UsePersistence<NoTimeoutsPersistence>();
                             config.UsePersistence<LearningPersistence, StorageType.Sagas>();
                             config.ReportSagaTimeoutsToServiceControl();
                         }

                         void SagasOnUnsupported(EndpointConfiguration config)
                         {
                             config.UsePersistence<LearningPersistence>();
                             config.UsePersistence<NoTimeoutsPersistence, StorageType.Sagas>();
                             [|config.ReportSagaTimeoutsToServiceControl()|];
                         }
                     }

                     {{NoTimeoutsPersistence}}
                     """;

        return Assert(source, DiagnosticIds.SagaTimeoutReportingRequiresSupportingPersistence);
    }

    [Test]
    public Task LaterPersistenceForAllStoragesTakesSagaStorage()
    {
        var source = $$"""
                     using NServiceBus;

                     class Configure
                     {
                         void UnsupportedTakesOver(EndpointConfiguration config)
                         {
                             config.UsePersistence<LearningPersistence, StorageType.Sagas>();
                             config.UsePersistence<NoTimeoutsPersistence>();
                             // NoTimeoutsPersistence might not support saga storage, so Learning could still provide it
                             config.ReportSagaTimeoutsToServiceControl();
                         }

                         void SupportedTakesOver(EndpointConfiguration config)
                         {
                             config.UsePersistence<NoTimeoutsPersistence, StorageType.Sagas>();
                             config.UsePersistence<LearningPersistence>();
                             config.ReportSagaTimeoutsToServiceControl();
                         }
                     }

                     {{NoTimeoutsPersistence}}
                     """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportWhenAnyCandidateSupportsTimeoutManagement()
    {
        var source = $$"""
                     using NServiceBus;

                     class Configure
                     {
                         void Endpoint(EndpointConfiguration config)
                         {
                             // Either could provide saga storage, which only Core knows
                             config.UsePersistence<NoTimeoutsPersistence>();
                             config.UsePersistence<LearningPersistence>();
                             config.ReportSagaTimeoutsToServiceControl();
                         }
                     }

                     {{NoTimeoutsPersistence}}
                     """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotCorrelateDifferentConfigurations()
    {
        var source = $$"""
                     using NServiceBus;

                     class Configure
                     {
                         void Endpoints(EndpointConfiguration sales, EndpointConfiguration billing)
                         {
                             sales.UsePersistence<NoTimeoutsPersistence>();
                             billing.UsePersistence<LearningPersistence>();
                             billing.ReportSagaTimeoutsToServiceControl();
                         }
                     }

                     {{NoTimeoutsPersistence}}
                     """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportWithoutPersistenceInTheSameMethod()
    {
        var source = """
                     using NServiceBus;

                     class Configure
                     {
                         void Endpoint(EndpointConfiguration config) => config.ReportSagaTimeoutsToServiceControl();
                     }
                     """;

        return Assert(source);
    }

    [Test]
    public Task ReportsFlaggedSagasInThisAssembly()
    {
        var source = $$"""
                     using NServiceBus;

                     [assembly: NServiceBus.Sagas.SagaUsesTimeoutManagement(typeof(OrderSaga))]

                     class Configure
                     {
                         void Endpoint(EndpointConfiguration config)
                         {
                             [|config.UsePersistence<NoTimeoutsPersistence>()|];
                         }

                         void Supported(EndpointConfiguration config)
                         {
                             config.UsePersistence<LearningPersistence>();
                         }
                     }

                     class OrderSaga;

                     {{NoTimeoutsPersistence}}
                     """;

        return Assert(source, DiagnosticIds.SagaTimeoutManagementRequiresSupportingPersistence);
    }

    [Test]
    public Task ReportsFlaggedSagasInReferencedAssemblies()
    {
        var library = CSharpCompilation.Create("SalesSagas",
            [CSharpSyntaxTree.ParseText("""
                                        [assembly: NServiceBus.Sagas.SagaUsesTimeoutManagement(typeof(Sales.OrderSaga))]
                                        namespace Sales { public class OrderSaga; }
                                        """)],
            SetUpFixture.ProjectReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var image = new MemoryStream();
        var emitResult = library.Emit(image);
        NUnit.Framework.Assert.That(emitResult.Success, Is.True, string.Join("\n", emitResult.Diagnostics));

        var source = $$"""
                     using NServiceBus;

                     class Configure
                     {
                         void Endpoint(EndpointConfiguration config)
                         {
                             [|config.UsePersistence<NoTimeoutsPersistence, StorageType.Sagas>()|];
                         }
                     }

                     {{NoTimeoutsPersistence}}
                     """;

        return AnalyzerTest.ForAnalyzer<SagaTimeoutManagementPersistenceAnalyzer>()
            .WithSource(source)
            .AddReferences(MetadataReference.CreateFromImage(image.ToArray()))
            .AssertDiagnostics(DiagnosticIds.SagaTimeoutManagementRequiresSupportingPersistence);
    }

    [Test]
    public async Task ReportsSagasFlaggedByTheGenerator()
    {
        var source = $$"""
                     using System.Threading.Tasks;
                     using NServiceBus;

                     class Configure
                     {
                         void Endpoint(EndpointConfiguration config)
                         {
                             config.UsePersistence<NoTimeoutsPersistence>();
                         }
                     }

                     [Saga]
                     public class OrderSaga : Saga<OrderData>, IAmStartedByMessages<CancelOrder>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<CancelOrder>(m => m.OrderId);

                         public Task Handle(CancelOrder message, IMessageHandlerContext context)
                         {
                             CancelTimeout(message.TimeoutId);
                             return Task.CompletedTask;
                         }
                     }

                     public class OrderData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class CancelOrder : ICommand
                     {
                         public string OrderId { get; set; }
                         public string TimeoutId { get; set; }
                     }

                     {{NoTimeoutsPersistence}}
                     """;

        // Runs the generator and then the analyzer on the resulting compilation, as the compiler does
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("Sales",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            SetUpFixture.ProjectReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CSharpGeneratorDriver.Create([new SagaTimeoutManagementUsageGenerator().AsSourceGenerator()], parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var generatedCompilation, out _);

        var diagnostics = await generatedCompilation
            .WithAnalyzers([new SagaTimeoutManagementPersistenceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();

        NUnit.Framework.Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { DiagnosticIds.SagaTimeoutManagementRequiresSupportingPersistence }));
        NUnit.Framework.Assert.That(diagnostics.Single().GetMessage(), Does.Contain("'OrderSaga'"));
    }

    const string NoTimeoutsPersistence = """
                                         public class NoTimeoutsPersistence : NServiceBus.Persistence.PersistenceDefinition, NServiceBus.Persistence.IPersistenceDefinitionFactory<NoTimeoutsPersistence>
                                         {
                                             static NoTimeoutsPersistence NServiceBus.Persistence.IPersistenceDefinitionFactory<NoTimeoutsPersistence>.Create() => new();
                                         }
                                         """;
}
