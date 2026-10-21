namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using Analyzer.Sagas;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class SagaTimeoutManagementUsageGeneratorTests
{
    [Test]
    public void FlagsSagasCallingTimeoutManagementApis()
    {
        var source = """
                     using System;
                     using System.Linq;
                     using System.Threading.Tasks;
                     using NServiceBus;

                     namespace Flagged
                     {
                         [Saga]
                         public class FindsTimeouts : Saga<OrderData>, IAmStartedByMessages<StartOrder>, IHandleTimeouts<BuyersRemorse>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<StartOrder>(m => m.OrderId);

                             public Task Handle(StartOrder message, IMessageHandlerContext context)
                             {
                                 var timeout = FindTimeouts<BuyersRemorse>(_ => true).FirstOrDefault();
                                 return Task.CompletedTask;
                             }

                             public Task Timeout(BuyersRemorse state, IMessageHandlerContext context) => Task.CompletedTask;
                         }

                         // The call is in another part of a partial saga
                         [Saga]
                         public partial class CancelsTimeouts : Saga<OrderData>, IAmStartedByMessages<StartOrder>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<StartOrder>(m => m.OrderId);
                         }

                         public partial class CancelsTimeouts
                         {
                             public Task Handle(StartOrder message, IMessageHandlerContext context)
                             {
                                 this.CancelTimeout("timeout-id");
                                 return Task.CompletedTask;
                             }
                         }

                         // The call is in a base saga in the same compilation
                         public abstract class ReschedulingSagaBase : Saga<OrderData>
                         {
                             protected void Postpone(string timeoutId) => RescheduleTimeout(timeoutId, DateTimeOffset.UtcNow.AddDays(1));
                         }

                         [Saga]
                         public class ReschedulesTimeouts : ReschedulingSagaBase, IAmStartedByMessages<StartOrder>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<StartOrder>(m => m.OrderId);

                             public Task Handle(StartOrder message, IMessageHandlerContext context)
                             {
                                 Postpone("timeout-id");
                                 return Task.CompletedTask;
                             }
                         }
                     }

                     namespace NotFlagged
                     {
                         // RequestTimeout works with every persister
                         [Saga]
                         public class RequestsTimeouts : Saga<Flagged.OrderData>, IAmStartedByMessages<Flagged.StartOrder>, IHandleTimeouts<Flagged.BuyersRemorse>
                         {
                             protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Flagged.OrderData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<Flagged.StartOrder>(m => m.OrderId);

                             public Task Handle(Flagged.StartOrder message, IMessageHandlerContext context) => RequestTimeout<Flagged.BuyersRemorse>(context, TimeSpan.FromDays(1));

                             public Task Timeout(Flagged.BuyersRemorse state, IMessageHandlerContext context) => Task.CompletedTask;

                             // A method with the same name that isn't the saga API
                             static void CancelTimeout(int ignored) { }

                             void Unrelated() => CancelTimeout(42);
                         }
                     }

                     namespace Flagged
                     {
                         public class OrderData : ContainSagaData
                         {
                             public string OrderId { get; set; }
                         }

                         public class StartOrder : ICommand
                         {
                             public string OrderId { get; set; }
                         }

                         public class BuyersRemorse;
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<SagaTimeoutManagementUsageGenerator>()
            .WithSource(source, "test.cs")
            .ControlOutput(GeneratorTestOutput.GeneratedOnly)
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void GeneratesNothingWithoutTimeoutManagement()
    {
        var source = """
                     using System;
                     using System.Threading.Tasks;
                     using NServiceBus;

                     [Saga]
                     public class RequestsTimeouts : Saga<OrderData>, IAmStartedByMessages<StartOrder>
                     {
                         protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<StartOrder>(m => m.OrderId);

                         public Task Handle(StartOrder message, IMessageHandlerContext context) => RequestTimeout<StartOrder>(context, TimeSpan.FromDays(1));
                     }

                     public class OrderData : ContainSagaData
                     {
                         public string OrderId { get; set; }
                     }

                     public class StartOrder : ICommand
                     {
                         public string OrderId { get; set; }
                     }
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<SagaTimeoutManagementUsageGenerator>()
            .WithSource(source, "test.cs")
            .Run()
            .ShouldNotGenerateCode();
    }
}
