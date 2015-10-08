﻿namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;
    using Pipeline;
    using ScenarioDescriptors;

    public class When_blowing_up_just_after_dispatch : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_still_release_the_outgoing_messages_to_the_transport()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(bus => bus.SendLocalAsync(new PlaceOrder())))
                .AllowSimulatedExceptions()
                .Done(c => c.OrderAckReceived == 1)
                .Repeat(r => r.For<AllOutboxCapableStorages>())
                .Should(context => Assert.AreEqual(1, context.OrderAckReceived, "Order ack should have been received since outbox dispatch isn't part of the receive tx"))
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
                        b.Pipeline.Register("BlowUpAfterDispatchBehavior", typeof(BlowUpAfterDispatchBehavior), "For testing");
                    });
            }

            class BlowUpAfterDispatchBehavior : Behavior<BatchDispatchContext>
            {
                static bool called;

                public override async Task Invoke(BatchDispatchContext context, Func<Task> next)
                {
                    if (!context.Operations.Any(op => op.Message.Headers[Headers.EnclosedMessageTypes].Contains(typeof(PlaceOrder).Name)))
                    {
                        await next().ConfigureAwait(false);
                        return;
                    }

                    if (called)
                    {
                        Console.Out.WriteLine("Called once, skipping next");
                        return;
                    }

                    await next().ConfigureAwait(false);

                    called = true;

                    throw new SimulatedException();
                }
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public IBus Bus { get; set; }

                public Task Handle(PlaceOrder message)
                {
                    return Bus.SendLocalAsync(new SendOrderAcknowledgment());
                }
            }

            class SendOrderAcknowledgmentHandler : IHandleMessages<SendOrderAcknowledgment>
            {
                public Context Context { get; set; }

                public Task Handle(SendOrderAcknowledgment message)
                {
                    Context.OrderAckReceived++;
                    return Task.FromResult(0);
                }
            }
        }


        public class PlaceOrder : ICommand
        {
        }

        class SendOrderAcknowledgment : IMessage
        {
        }
    }
}