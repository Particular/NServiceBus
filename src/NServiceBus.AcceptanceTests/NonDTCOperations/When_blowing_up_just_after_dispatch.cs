namespace NServiceBus.AcceptanceTests.NonDTCOperations
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;
    using Pipeline;
    using Pipeline.Contexts;
    using ScenarioDescriptors;

    public class When_blowing_up_just_after_dispatch : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_still_release_the_outgoing_messages_to_the_transport()
        {
          
            Scenario.Define<Context>()
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                    .AllowExceptions()
                    .Done(c => c.OrderAckReceived == 1)
                    .Repeat(r=>r.For<AllOutboxCapableStorages>())
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
                EndpointSetup<DefaultServer>(c =>{},
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableOutbox();
                        b.Pipeline.Register<BlowUpAfterDispatchBehavior.Registration>();
                        b.RegisterComponents(r => r.ConfigureComponent<BlowUpAfterDispatchBehavior>(DependencyLifecycle.InstancePerCall));
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
        public class Registration : RegisterStep
        {
            public Registration() : base("BlowUpAfterDispatchBehavior", typeof(BlowUpAfterDispatchBehavior), "For testing")
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
