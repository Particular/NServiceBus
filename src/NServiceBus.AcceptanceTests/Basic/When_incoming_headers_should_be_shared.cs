namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_incoming_headers_should_be_shared : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_expose_header_in_downstream_handlers()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new Message())))
                .Done(c => c.GotMessage)
                .Run();

            Assert.IsTrue(context.SecondHandlerCanReadHeaderSetByFirstHandler);
        }

        public class Context : ScenarioContext
        {
            public bool SecondHandlerCanReadHeaderSetByFirstHandler { get; set; }
            public bool GotMessage   { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class EnsureOrdering : ISpecifyMessageHandlerOrdering
            {
                public void SpecifyOrder(Order order)
                {
                    order.Specify(First<FirstHandler>.Then<SecondHandler>());
                }
            }
       
            class FirstHandler : IHandleMessages<Message>
            {
                public IBus Bus { get; set; }

                public void Handle(Message message)
                {
                    Bus.SetMessageHeader(message, "Key", "Value");
                }
            }

            class SecondHandler : IHandleMessages<Message>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public void Handle(Message message)
                {
                    var header = Bus.GetMessageHeader(message, "Key");
                    Context.SecondHandlerCanReadHeaderSetByFirstHandler = header == "Value";
                    Context.GotMessage = true;
                }
            }
        }

        [Serializable]
        public class Message : ICommand
        {
        }

    }
}