﻿namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_blowing_up_just_after_dispatch : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_still_release_the_outgoing_messages_to_the_transport()
        {
            Requires.OutboxPersistence();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b
                    .DoNotFailOnErrorMessages() // PlaceOrder should fail due to exception after dispatch
                    .When(session => session.SendLocal(new PlaceOrder())))
                .Done(c => c.OrderAckReceived == 1)
                .Run(TimeSpan.FromSeconds(20));

            Assert.AreEqual(1, context.OrderAckReceived, "Order ack should have been received since outbox dispatch isn't part of the receive tx");
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
                        b.EnableOutbox();
                        b.Pipeline.Register("BlowUpAfterDispatchBehavior", new BlowUpAfterDispatchBehavior(), "For testing");
                    });
            }

            class BlowUpAfterDispatchBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
            {
                public async Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
                {
                    await next(context).ConfigureAwait(false);

                    throw new SimulatedException();
                }
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public Task Handle(PlaceOrder message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new SendOrderAcknowledgment());
                }
            }

            class SendOrderAcknowledgmentHandler : IHandleMessages<SendOrderAcknowledgment>
            {
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgment message, IMessageHandlerContext context)
                {
                    Context.OrderAckReceived++;
                    return Task.FromResult(0);
                }
            }
        }

        public class PlaceOrder : ICommand
        {
        }

        public class SendOrderAcknowledgment : IMessage
        {
        }
    }
}