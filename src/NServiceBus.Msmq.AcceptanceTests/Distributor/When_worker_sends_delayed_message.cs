namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using EndpointTemplates;

    public class When_worker_sends_delayed_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_distributor_timeout_manager()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Worker>()
                .WithEndpoint<Distributor>(e => e
                    .When(c => c.IsWorkerRegistered, s => s.Send(new DispatchDelayedMessage())))
                .Done(c => c.DistributorReceivedDelayedMessage)
                .Run();

            Assert.IsTrue(context.DistributorReceivedDelayedMessage);
            Assert.IsFalse(context.WorkerReceivedDelayedMessage);
        }

        class Context : DistributorEndpointTemplate.DistributorContext
        {
            public bool DistributorReceivedDelayedMessage { get; set; }
            public bool WorkerReceivedDelayedMessage { get; set; }
        }

        class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<DefaultServer>(c => c.EnlistWithDistributor(typeof(Distributor)));
            }

            class DelayedMessageHandler : IHandleMessages<DelayedMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DelayedMessage message, IMessageHandlerContext context)
                {
                    testContext.WorkerReceivedDelayedMessage = true;
                    return Task.CompletedTask;
                }
            }

            class DispatchDelayedMessageHandler : IHandleMessages<DispatchDelayedMessage>
            {
                public Task Handle(DispatchDelayedMessage message, IMessageHandlerContext context)
                {
                    var sendOptions = new SendOptions();
                    sendOptions.DelayDeliveryWith(TimeSpan.FromSeconds(1));
                    sendOptions.RouteToThisEndpoint();
                    return context.Send(new DelayedMessage(), sendOptions);
                }
            }
        }

        class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DistributorEndpointTemplate>(c =>
                {
                }).AddMapping<DispatchDelayedMessage>(typeof(Worker));
            }

            class DelayedMessageHandler : IHandleMessages<DelayedMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DelayedMessage message, IMessageHandlerContext context)
                {
                    testContext.DistributorReceivedDelayedMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        class DelayedMessage : ICommand
        {
        }

        class DispatchDelayedMessage : ICommand
        {
        }
    }
}