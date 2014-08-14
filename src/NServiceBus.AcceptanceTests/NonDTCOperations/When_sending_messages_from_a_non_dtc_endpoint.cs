﻿namespace NServiceBus.AcceptanceTests.NonDTCOperations
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_messages_from_a_non_dtc_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_store_them_and_dispatch_them_from_the_outbox()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<NonDtcSalesEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                    .Done(c => c.OrderAckReceived)
                    .Repeat(r=>r.For<AllOutboxCapableStorages>())
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
                EndpointSetup<DefaultServer>(c => c.Settings.Set("DisableOutboxTransportCheck", true),
                builder => builder.EnableOutbox());
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
        class PlaceOrder : ICommand { }

        [Serializable]
        class SendOrderAcknowledgement : IMessage { }
    }
}
