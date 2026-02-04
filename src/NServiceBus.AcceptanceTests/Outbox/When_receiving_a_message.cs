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
            .Run(cancellationToken);

        Assert.That(context.OrderAckReceived, Is.EqualTo(1), "Order ack should have been received");
    }

    public class Context : ScenarioContext
    {
        public int OrderAckReceived { get; set; }
    }

    public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
    {
        public NonDtcReceivingEndpoint() =>
            EndpointSetup<DefaultServer>(b =>
            {
                b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                b.EnableOutbox();
            });

        [Handler]
        public class PlaceOrderHandler : IHandleMessages<PlaceOrder>
        {
            public Task Handle(PlaceOrder message, IMessageHandlerContext context) => context.SendLocal(new SendOrderAcknowledgement());
        }

        [Handler]
        public class SendOrderAcknowledgementHandler(Context testContext) : IHandleMessages<SendOrderAcknowledgement>
        {
            public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
            {
                testContext.OrderAckReceived++;
                testContext.MarkAsCompleted(testContext.OrderAckReceived == 1);
                return Task.CompletedTask;
            }
        }
    }

    public class PlaceOrder : ICommand;

    public class SendOrderAcknowledgement : IMessage;
}