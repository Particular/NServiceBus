namespace NServiceBus.AcceptanceTests.NonDTCOperations
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_receiving_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_handle_it()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                    .Done(c => context.OrderAckReceived == 1)
                    .Run();
        }

        [Test]
        public void Should_discard_duplicates_using_the_outbox()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus =>
                    {
                        var duplicateMessageId = Guid.NewGuid().ToString();
                        bus.SendLocal<PlaceOrder>(m => m.SetHeader(Headers.MessageId, duplicateMessageId));
                        bus.SendLocal<PlaceOrder>(m => m.SetHeader(Headers.MessageId, duplicateMessageId));
                        bus.SendLocal(new PlaceOrder());
                    }))
                    .Done(c => context.OrderAckReceived >= 2)
                    .Run();

            Assert.AreEqual(2, context.OrderAckReceived);
        }

        public class Context : ScenarioContext
        {
            public int OrderAckReceived { get; set; }
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    Configure.Transactions.Advanced(t =>
                    {
                        t.DisableDistributedTransactions();
                        t.DoNotWrapHandlersExecutionInATransactionScope();
                    });

                    c.Features.Enable<Features.Outbox>();
                })
                .AllowExceptions();
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
