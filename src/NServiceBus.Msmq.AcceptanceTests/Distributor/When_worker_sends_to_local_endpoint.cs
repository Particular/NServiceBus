namespace NServiceBus.AcceptanceTests.Distributor
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_worker_sends_to_local_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_messages_to_distributor()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Distributor>()
                .WithEndpoint<Worker>(b => b
                    .When(async s =>
                    {
                        await s.SendLocal(new SendLocalMessage());
                        var sendOptions = new SendOptions();
                        sendOptions.RouteToThisEndpoint();
                        await s.Send(new RouteToThisEndpointMessage(), sendOptions);
                    }))
                .Done(c => c.ReceivedRouteToThisEndpointMessage && c.ReceivedSendLocalMessage)
                .Run();

            Assert.IsTrue(context.ReceivedRouteToThisEndpointMessage);
            Assert.IsTrue(context.ReceivedSendLocalMessage);
        }

        class Context : DistributorEndpointTemplate.DistributorContext
        {
            public bool ReceivedSendLocalMessage { get; set; }
            public bool ReceivedRouteToThisEndpointMessage { get; set; }
        }

        class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DistributorEndpointTemplate>();
            }

            class SendLocalMessageHandler : IHandleMessages<SendLocalMessage>
            {
                Context testContext;

                public SendLocalMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SendLocalMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedSendLocalMessage = true;
                    return Task.CompletedTask;
                }
            }

            class RouteToThisEndpointMessageHandler : IHandleMessages<RouteToThisEndpointMessage>
            {
                Context testContext;

                public RouteToThisEndpointMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(RouteToThisEndpointMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedRouteToThisEndpointMessage = true;
                    return Task.CompletedTask;
                }
            }
        }

        class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<DefaultServer>(c => c.EnlistWithDistributor(typeof(Distributor)));
            }
        }

        class SendLocalMessage : ICommand
        {
        }

        class RouteToThisEndpointMessage : ICommand
        {
        }
    }
}