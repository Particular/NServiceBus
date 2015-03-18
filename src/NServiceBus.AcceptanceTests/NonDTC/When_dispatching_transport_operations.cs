namespace NServiceBus.AcceptanceTests.NonDTC
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NUnit.Framework;

    public class When_dispatching_transport_operations : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_honor_all_delivery_options()
        {
          
            Scenario.Define<Context>()
                    .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder())))
                   .Done(c => c.DispatchedMessageReceived)
                    .Repeat(r=>r.For<AllOutboxCapableStorages>())
                    .Should(context =>
                    {
                        Assert.AreEqual(TimeSpan.FromMinutes(1), TimeSpan.Parse(context.HeadersOnDispatchedMessage[Headers.TimeToBeReceived]), "Should honor the TTBR");
                        Assert.True(bool.Parse(context.HeadersOnDispatchedMessage[Headers.NonDurableMessage]), "Should honor the durability");
                    })
                    .Run(TimeSpan.FromSeconds(20));
        }



        public class Context : ScenarioContext
        {
            public bool DispatchedMessageReceived { get; set; }
            public IDictionary<string, string> HeadersOnDispatchedMessage { get; set; }
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
                    });
            }

            class PlaceOrderHandler : IHandleMessages<PlaceOrder>
            {
                public IBus Bus { get; set; }

                public void Handle(PlaceOrder message)
                {
                    Bus.SendLocal(new MessageToDispatch());
                }
            }

            class MessageToDispatchHandler : IHandleMessages<MessageToDispatch>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MessageToDispatch message)
                {
                    Context.HeadersOnDispatchedMessage = Bus.CurrentMessageContext.Headers;
                    Context.DispatchedMessageReceived = true;
                }
            }
        }


        public class PlaceOrder : ICommand { }

        [TimeToBeReceived("00:01:00")]
        [Express]
        class MessageToDispatch : IMessage { }
    }

    
}
