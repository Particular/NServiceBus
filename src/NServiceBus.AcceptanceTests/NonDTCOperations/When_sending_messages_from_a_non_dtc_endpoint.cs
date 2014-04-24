﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Outbox;
    using Persistence.InMemory.Outbox;

    public class When_sending_messages_from_a_non_dtc_endpoint : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_store_them_and_dispatch_them_from_the_outbox()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<NonDtcSalesEndpoint>(b => b.Given(bus =>
                    {
                        bus.SendLocal(new PlaceOrder());
                    }))
                    .Done(c => context.OrderAckReceived)
                    .Run();

        }


        public class Context : ScenarioContext
        {
            public bool OrderAckReceived { get; set; }
        }


        public class NonDtcSalesEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcSalesEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    InfrastructureServices.SetDefaultFor<IOutboxStorage>(() =>
                        Configure.Component<InMemoryOutboxStorage>(DependencyLifecycle.SingleInstance));

                    Configure.Features.Enable<Features.Outbox>();
                });
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(PlaceOrder message)
                {
                    Bus.SendLocal(new SendOrderAcknowledgement());
                }
            }

            class SendOrderAcknowledgementHandler : IHandleMessages<SendOrderAcknowledgement>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(SendOrderAcknowledgement message)
                {
                    Context.OrderAckReceived = true;
                }
            }

        }

        [Serializable]
        class PlaceOrder : ICommand
        {
            public string OrderId { get; set; }
            public bool Duplicate { get; set; }
        }

        [Serializable]
        class SendOrderAcknowledgement : IMessage
        {
        }

    }

}
