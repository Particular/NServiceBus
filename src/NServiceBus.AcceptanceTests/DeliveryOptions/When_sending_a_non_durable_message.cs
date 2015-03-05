namespace NServiceBus.AcceptanceTests.DeliveryOptions
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_a_non_durable_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_availble_as_a_header_on_receiver()
        {
            var context = new Context();
            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new MyMessage())))
                    .Done(c=>c.WasCalled)
                    .Run(TimeSpan.FromSeconds(10));
        
            Assert.IsTrue(context.NonDurabilityHeader,"Message should be flagged as non durable");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public bool NonDurabilityHeader { get; set; }
        }
        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.NonDurabilityHeader = bool.Parse(Bus.CurrentMessageContext.Headers[Headers.NonDurableMessage]);
                    Context.WasCalled = true;
                }
            }
        }

      
        [Express]
        public class MyMessage : IMessage
        {
        }
    }
}
