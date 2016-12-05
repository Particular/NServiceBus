namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using EndpointTemplates;

    public class When_worker_uses_delayed_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_distributor_timeout_manager_and_also_send_a_ready_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Worker>()
                .WithEndpoint<Distributor>(e => e
                    .When(c => c.IsWorkerRegistered, s => s.Send(new FailingMessage())))
                .Done(c => c.DistributorReceivedRetry && c.ReceivedReadyMessage)
                .Run();

            Assert.IsTrue(context.DistributorReceivedRetry);
            Assert.IsTrue(context.ReceivedReadyMessage);
        }

        class Context : DistributorEndpointTemplate.DistributorContext
        {
            public bool DistributorReceivedRetry { get; set; }
        }

        class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnlistWithDistributor(typeof(Distributor));
                    c.Recoverability()
                        .Immediate(s => s
                            .NumberOfRetries(0))
                        .Delayed(s => s
                            .NumberOfRetries(1)
                            .TimeIncrease(TimeSpan.Zero));
                });
            }

            class DelayedMessageHandler : IHandleMessages<FailingMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DistributorEndpointTemplate>()
                    .AddMapping<FailingMessage>(typeof(Worker));
            }

            class DelayedMessageHandler : IHandleMessages<FailingMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    testContext.DistributorReceivedRetry = true;
                    return Task.CompletedTask;
                }
            }
        }

        class FailingMessage : ICommand
        {
        }
    }
}