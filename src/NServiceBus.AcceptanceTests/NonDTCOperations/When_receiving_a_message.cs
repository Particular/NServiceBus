namespace NServiceBus.AcceptanceTests.NonDTCOperations
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_receiving_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_handle_it()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                    .AllowExceptions()
                    .Done(c => c.OrderAckReceived == 1)
                    .Repeat(r => r.For<AllOutboxCapableStorages>())
                    .Run(TimeSpan.FromSeconds(20));
        }

        [Test]
        public void Should_discard_duplicates_using_the_outbox()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus =>
                    {
                        var duplicateMessageId = Guid.NewGuid().ToString();
                        bus.SendLocal<PlaceOrder>(m => bus.SetMessageHeader(m, Headers.MessageId, duplicateMessageId));
                        bus.SendLocal<PlaceOrder>(m => bus.SetMessageHeader(m, Headers.MessageId, duplicateMessageId));
                        bus.SendLocal(new PlaceOrder());
                    }))
                    .AllowExceptions()
                    .Done(c => c.OrderAckReceived >= 2)
                    .Repeat(r => r.For<AllOutboxCapableStorages>())
                    .Should(context => Assert.AreEqual(2, context.OrderAckReceived))
                    .Run(TimeSpan.FromSeconds(20));
        }

        public class Context : ScenarioContext
        {
            public int OrderAckReceived { get; set; }
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableOutbox();
                    })
                .AuditTo("audit");
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
                    Context.OrderAckReceived++;
                }
            }
        }


        [Serializable]
        class PlaceOrder : ICommand { }

        [Serializable]
        class SendOrderAcknowledgement : IMessage { }
    }
}
