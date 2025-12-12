namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

[NServiceBusRegistrations]
public class When_manually_registering_saga_with_timeouts : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_register_timeout_handler_with_manually_registered_saga()
    {
        var id = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<ManualTimeoutSagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaWithTimeout
            {
                OrderId = id
            })))
            .Done(c => c.SagaCompleted)
            .Run();

        Assert.That(context.SagaCompleted, Is.True);
        Assert.That(context.OrderId, Is.EqualTo(id));
    }

    public class Context : ScenarioContext
    {
        public bool SagaCompleted { get; set; }
        public Guid OrderId { get; set; }
    }

    public class ManualTimeoutSagaEndpoint : EndpointConfigurationBuilder
    {
        public ManualTimeoutSagaEndpoint() =>
            EndpointSetup<NonScanningServer>(config =>
            {
                // Manually register the saga using AddSaga
                config.AddSaga<TimeoutHandlingSaga>();
            });

        public class TimeoutHandlingSaga(Context testContext)
            : Saga<TimeoutHandlingSagaData>,
              IAmStartedByMessages<StartSagaWithTimeout>,
              IHandleTimeouts<OrderProcessingTimeout>
        {
            public async Task Handle(StartSagaWithTimeout message, IMessageHandlerContext context)
            {
                Data.OrderId = message.OrderId;
                testContext.OrderId = Data.OrderId;
                await RequestTimeout(context, TimeSpan.FromMilliseconds(1), new OrderProcessingTimeout());
            }

            public Task Timeout(OrderProcessingTimeout state, IMessageHandlerContext context)
            {
                testContext.SagaCompleted = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutHandlingSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartSagaWithTimeout>(m => m.OrderId);
        }

        public class TimeoutHandlingSagaData : ContainSagaData
        {
            public virtual Guid OrderId { get; set; }
        }
    }

    public class StartSagaWithTimeout : ICommand
    {
        public Guid OrderId { get; set; }
    }

    public class OrderProcessingTimeout;
}