namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_a_message_not_found_in_the_outbox : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_handle_it()
        {
            Requires.OutboxPersistence();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.OrderAckReceived == 1)
                .Run(TimeSpan.FromSeconds(20));

            Assert.AreEqual(1, context.OrderAckReceived, "Order ack should have been received");
        }

        class Context : ScenarioContext
        {
            public int OrderAckReceived { get; set; }
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableOutbox();
                });
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new SendOrderAcknowledgement());
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public SendOrderAcknowledgementHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    testContext.OrderAckReceived++;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class PlaceOrder : ICommand
        {
        }

        public class SendOrderAcknowledgement : IMessage
        {
        }
    }
}