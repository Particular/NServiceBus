namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_sending_messages_from_a_non_dtc_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_store_them_and_dispatch_them_from_the_outbox()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<NonDtcSalesEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                    .Done(c => context.OrderAckReceived)
                    .Run(TimeSpan.FromSeconds(20));

            Assert.IsTrue(context.OrderAckReceived);
        }

        public class Context : ScenarioContext
        {
            public bool OrderAckReceived { get; set; }
        }

        public class NonDtcSalesEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcSalesEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.EnableOutbox());
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public IBus Bus { get; set; }

                public void Handle(PlaceOrder message)
                {
                    Bus.SendLocal(new SendOrderAcknowledgement());
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public void Handle(SendOrderAcknowledgement message)
                {
                    Context.OrderAckReceived = true;
                }
            }
        }

        [Serializable]
        class PlaceOrder : ICommand{}

        [Serializable]
        class SendOrderAcknowledgement : IMessage{}
    }
}
