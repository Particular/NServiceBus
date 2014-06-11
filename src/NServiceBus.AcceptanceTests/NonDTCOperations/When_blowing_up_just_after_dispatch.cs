namespace NServiceBus.AcceptanceTests.NonDTCOperations
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;

    public class When_blowing_up_just_after_dispatch : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_still_release_the_outgoing_messages_to_the_transport()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                    .AllowExceptions()
                    .Done(c => context.OrderAckReceived == 1)
                    .Run(TimeSpan.FromSeconds(10));

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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Settings.Set("DisableOutboxTransportCheck", true);
                    c.EnableOutbox();
                    c.Pipeline.Register<BlowUpAfterDispatchBehavior.Registration>();
                    c.Configurer.ConfigureComponent<BlowUpAfterDispatchBehavior>(DependencyLifecycle.InstancePerCall);
                });

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
        public class PlaceOrder : ICommand { }

        [Serializable]
        class SendOrderAcknowledgement : IMessage { }
    }

    public class BlowUpAfterDispatchBehavior : IBehavior<IncomingContext>
    {
        public class Registration : RegisterBehavior
        {
            public Registration()
                : base("BlowUpAfterDispatchBehavior", typeof(BlowUpAfterDispatchBehavior), "For testing")
            {
                InsertBefore("OutboxDeduplication");
            }
        }

        public void Invoke(IncomingContext context, Action next)
        {
            if (!context.PhysicalMessage.Headers[Headers.EnclosedMessageTypes].Contains(typeof(When_blowing_up_just_after_dispatch.PlaceOrder).Name))
            {
                next();
                return;
            }


            if (called)
            {
                Console.Out.WriteLine("Called once, skipping next");
                return;

            }
            else
            {
                next();
            }


            called = true;

            throw new Exception("Fake ex after dispatch");
        }

        static bool called;
    }
}
