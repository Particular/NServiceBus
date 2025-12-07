namespace NServiceBus.AcceptanceTests.Outbox;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_receiving_a_message_not_found_in_the_outbox : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(20_000)]
    public async Task Should_handle_it(CancellationToken cancellationToken = default)
    {
        Requires.OutboxPersistence();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(session => session.SendLocal(new PlaceOrder())))
            .Done(c => c.OrderAckReceived == 1)
            .Run(cancellationToken);

        Assert.That(context.OrderAckReceived, Is.EqualTo(1), "Order ack should have been received");
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
                b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
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
                return Task.CompletedTask;
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