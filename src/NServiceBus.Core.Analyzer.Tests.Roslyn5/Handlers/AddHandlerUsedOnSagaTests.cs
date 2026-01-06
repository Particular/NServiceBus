namespace NServiceBus.Core.Analyzer.Tests.Handlers;

using System.Threading.Tasks;
using Analyzer.Handlers;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class AddHandlerUsedOnSagaTests : AnalyzerTestFixture<AddHandlerOnSagaTypeAnalyzer>
{
    [Test]
    public Task MultiTest()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             // No call on a regular handler
                             cfg.AddHandler<MyHandler>();
                             
                             // Can't call AddHandler on a Saga
                             cfg.[|AddHandler<MySaga>|]();
                             
                             // Don't need to check AddSaga<MyHandler>(), generic constraint prevents this
                         }
                     }

                     public class MyHandler : IHandleMessages<Cmd>
                     {
                         public Task Handle(Cmd cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class MySaga : Saga<MyData>, IAmStartedByMessages<Cmd>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
                         {
                             mapper.MapSaga(s => s.Corr)
                                .ToMessage<Cmd>(c => c.Corr);
                         }
                         
                         public Task Handle(Cmd cmd, IMessageHandlerContext context) => Task.CompletedTask;
                     }

                     public class MyData : ContainSagaData
                     {
                         public string Corr { get; set; }
                     }
                     public class Cmd : ICommand
                     {
                         public string Corr { get; set; }
                     }
                     """;

        return Assert(DiagnosticIds.AddHandlerOnSagaType, source);
    }
}