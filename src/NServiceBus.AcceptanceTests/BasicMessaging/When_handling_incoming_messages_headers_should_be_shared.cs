namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    [Explicit]
    public class When_handling_incoming_messages_headers_should_be_shared : NServiceBusAcceptanceTest
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

                public void Handle(Message message)
                {
                    message.SetHeader("Key", "Value");
                }
            }

            class SecondHandler : IHandleMessages<Message>
            {
                public Context Context { get; set; }

                public void Handle(Message message)
                {
                    var header = message.GetHeader("Key");
                    Context.GotMessage = true;
                    Context.SecondHandlerCanReadHeaderSetByFirstHandler = header == "Value";
                }
            }
        }

        [Serializable]
        public class Message : ICommand
        {
        }

    }
}