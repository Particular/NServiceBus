namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using Analyzer.Sagas;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class AddSagaInterceptorSuppressorTests
{
    [Test]
    public void SuppressesIL2026ForAddSaga()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.AddSaga<SampleSaga>();
                         }
                     }

                     public class SampleSaga : Saga<SampleSagaData>,
                         IAmStartedByMessages<SampleCommand>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SampleSagaData> mapper)
                         {
                             mapper.MapSaga(saga => saga.CorrelationId)
                                 .ToMessage<SampleCommand>(msg => msg.CorrelationId);
                         }

                         public Task Handle(SampleCommand cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class SampleSagaData : ContainSagaData
                     {
                         public string CorrelationId { get; set; }
                     }

                     public class SampleCommand : ICommand
                     {
                         public string CorrelationId { get; set; }
                     }
                     """;

        var result = SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithAnalyzer<MockTrimmingAnalyzer>()
            .WithSuppressor<AddSagaInterceptorSuppressor>()
            .Run();

        var diagnostics = result.GetCompilationOutput();

        Assert.That(diagnostics, Does.Not.Contain("IL2026"));
    }

    [Test]
    public void DoesNotSuppressIL2026ForNonAddSagaCalls()
    {
        var source = """
                     using System.Diagnostics.CodeAnalysis;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             // This call should still produce IL2026 since it's not intercepted
                             SomeOtherMethod();
                         }

                         [RequiresUnreferencedCode("Test method")]
                         public void SomeOtherMethod() { }
                     }
                     """;

        var result = SourceGeneratorTest.ForIncrementalGenerator<AddSagaInterceptor>()
            .WithSource(source, "test.cs")
            .WithAnalyzer<MockTrimmingAnalyzer>()
            .WithSuppressor<AddSagaInterceptorSuppressor>()
            .SuppressDiagnosticErrors()
            .SuppressCompilationErrors()
            .Run();

        var diagnostics = result.GetCompilationOutput();

        Assert.That(diagnostics, Does.Contain("IL2026"));
    }
}