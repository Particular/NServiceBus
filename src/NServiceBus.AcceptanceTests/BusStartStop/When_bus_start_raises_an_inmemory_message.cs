namespace NServiceBus.AcceptanceTests.BusStartStop
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

#pragma warning disable 612, 618
    public class When_bus_start_raises_an_inmemory_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_throw()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>()
                .Done(c => c.GotMessage)
                .Repeat(r => r.For<AllBuilders>())
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotMessage;
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MessageHandler : IHandleMessages<Message>
            {
                public Context Context { get; set; }

                public void Handle(Message message)
                {
                    Context.GotMessage = true;
                }
            }
        }

        public class Foo : IWantToRunWhenBusStartsAndStops
        {
            public IBus Bus { get; set; }

            public void Start()
            {
                Bus.InMemory.Raise(new Message());
            }

            public void Stop()
            {
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }

    }
#pragma warning restore  612, 618

}