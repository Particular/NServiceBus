namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NUnit.Framework;
using AcceptanceTesting.Customization;

public class When_atomic_outbox : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_deliver_messages()
    {
        Requires.OutboxPersistence();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<AtomicOutboxEndpoint>(b => b
                .When(session => session.SendLocal(new PlaceOrder())))
            .WithEndpoint<DownstreamEndpoint>()
            .Done(c => c.SendOrderAcknowledgementReceived > 0)
            .Run(TimeSpan.FromSeconds(20));

        Assert.That(context.SendOrderAcknowledgementReceived, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_mark_outbox_record_as_dispatched()
    {
        //HINT: The test scenario sends a duplicate of the PlaceOrder message once the first
        //      copy has been fully processed, followed by a marker PlaceOrderFollowUp message.
        //      Because the outbox has been marked as Dispatched via a control message
        //      the duplicate copy does not trigger re-sending SendOrderAcknowledgement.
        //      If it did, the re-sent message would arrive before the final SendOrderAcknowledgementFollowUp
        Requires.OutboxPersistence();

        var messageId = Guid.NewGuid().ToString();
        var context = await Scenario.Define<Context>()
            .WithEndpoint<AtomicOutboxEndpoint>(b => b
                .When(session =>
                {
                    var ops = new SendOptions();
                    ops.SetMessageId(messageId);
                    ops.RouteToThisEndpoint();
                    return session.Send(new PlaceOrder(), ops);
                })
                .When(ctx => ctx.SendOrderAcknowledgementReceived > 0, async session =>
                {
                    var ops = new SendOptions();
                    ops.SetMessageId(messageId);
                    ops.RouteToThisEndpoint();
                    await session.Send(new PlaceOrder(), ops);
                    await session.SendLocal(new PlaceOrderFollowUp());
                }))
            .WithEndpoint<DownstreamEndpoint>()
            .Done(c => c.SendOrderAcknowledgementFollowUpReceived)
            .Run(TimeSpan.FromSeconds(20));

        Assert.That(context.SendOrderAcknowledgementFollowUpReceived, Is.True);
        Assert.That(context.SendOrderAcknowledgementReceived, Is.EqualTo(1));
    }

    class Context : ScenarioContext
    {
        public int SendOrderAcknowledgementReceived { get; set; }
        public bool SendOrderAcknowledgementFollowUpReceived { get; set; }
    }

    public class DownstreamEndpoint : EndpointConfigurationBuilder
    {
        public DownstreamEndpoint()
        {
            EndpointSetup<DefaultServer>(b =>
            {
                b.LimitMessageProcessingConcurrencyTo(1);
                b.ConfigureTransport<AcceptanceTestingTransport>().FifoMode = true;
            });
        }

        class SendOrderAcknowledgementFollowUpHandler(Context testContext)
            : IHandleMessages<SendOrderAcknowledgementFollowUp>
        {
            public Task Handle(SendOrderAcknowledgementFollowUp message, IMessageHandlerContext context)
            {
                testContext.SendOrderAcknowledgementFollowUpReceived = true;
                return Task.CompletedTask;
            }
        }

        class SendOrderAcknowledgementHandler(Context testContext)
            : IHandleMessages<SendOrderAcknowledgement>
        {
            public Task Handle(SendOrderAcknowledgement message, IMessageHandlerContext context)
            {
                testContext.SendOrderAcknowledgementReceived++;
                return Task.CompletedTask;
            }
        }
    }

    public class AtomicOutboxEndpoint : EndpointConfigurationBuilder
    {
        public AtomicOutboxEndpoint()
        {
            EndpointSetup<DefaultServer>(b =>
            {
                b.EnableOutbox();
                b.LimitMessageProcessingConcurrencyTo(1);
                b.ConfigureTransport<AcceptanceTestingTransport>().FifoMode = true;
                b.GetSettings().Set("Outbox.AllowSendsAtomicWithReceive", true);
                b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
                var routing = b.ConfigureRouting();
                routing.RouteToEndpoint(typeof(SendOrderAcknowledgement), typeof(DownstreamEndpoint));
                routing.RouteToEndpoint(typeof(SendOrderAcknowledgementFollowUp), typeof(DownstreamEndpoint));
            });
        }

        class PlaceOrderHandler : IHandleMessages<PlaceOrder>
        {
            public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                => context.Send(new SendOrderAcknowledgement());
        }

        class PlaceOrderFollowUpHandler : IHandleMessages<PlaceOrderFollowUp>
        {
            public Task Handle(PlaceOrderFollowUp message, IMessageHandlerContext context)
                => context.Send(new SendOrderAcknowledgementFollowUp());
        }
    }

    public class PlaceOrder : ICommand
    {
    }

    public class PlaceOrderFollowUp : ICommand
    {
    }

    public class SendOrderAcknowledgement : IMessage
    {
    }

    public class SendOrderAcknowledgementFollowUp : IMessage
    {
    }
}