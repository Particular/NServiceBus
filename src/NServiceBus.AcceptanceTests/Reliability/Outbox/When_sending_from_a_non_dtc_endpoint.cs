namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_from_a_non_dtc_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_store_them_and_dispatch_them_from_the_outbox()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<NonDtcSalesEndpoint>(b => b.When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.OrderAckReceived)
                .Repeat(r => r.For<AllOutboxCapableStorages>())
                .Should(context => Assert.IsTrue(context.OrderAckReceived))
                .Run(TimeSpan.FromSeconds(20));
        }

        public class Context : ScenarioContext
        {
            public bool OrderAckReceived { get; set; }
        }

        public class NonDtcSalesEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcSalesEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
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
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
                {
                    Context.OrderAckReceived = true;
                    return Task.FromResult(0);
                }
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