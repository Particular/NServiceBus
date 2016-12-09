namespace NServiceBus.Transport.Msmq.AcceptanceTests.Distributor
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_worker_sends_to_local_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_messages_to_distributor()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Distributor>(b => b
                    .When(
                        c => c.IsWorkerRegistered,
                        s => s.Send(new DispatchMessages())))
                .WithEndpoint<Worker>()
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
                EndpointSetup<DistributorEndpointTemplate>().AddMapping<DispatchMessages>(typeof(Worker));
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

            class DispatchMessageHandler : IHandleMessages<DispatchMessages>
            {
                public async Task Handle(DispatchMessages message, IMessageHandlerContext context)
                {
                    await context.SendLocal(new SendLocalMessage());
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    await context.Send(new RouteToThisEndpointMessage(), sendOptions);
                }
            }
        }

        class DispatchMessages : ICommand
        {
        }

        class SendLocalMessage : ICommand
        {
        }

        class RouteToThisEndpointMessage : ICommand
        {
        }
    }
}