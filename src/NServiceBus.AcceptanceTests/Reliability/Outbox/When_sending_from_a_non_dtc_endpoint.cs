﻿namespace NServiceBus.AcceptanceTests.Reliability.Outbox
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
        public async Task Should_store_them_and_dispatch_them_from_the_outbox()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<NonDtcSalesEndpoint>(b => b.When(bus => bus.SendLocalAsync(new PlaceOrder())))
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
                public IBus Bus { get; set; }

                public Task Handle(PlaceOrder message)
                {
                    return Bus.SendLocalAsync(new SendOrderAcknowledgement());
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgement message)
                {
                    Context.OrderAckReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        class PlaceOrder : ICommand
        {
        }

        [Serializable]
        class SendOrderAcknowledgement : IMessage
        {
        }
    }
}