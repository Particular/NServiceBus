namespace NServiceBus.AcceptanceTests.InMemory
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_raising_an_in_memory_event : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_delivered_to_handlers()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<InMemoryEndpoint>(b => b.Given((bus, c) => bus.SendLocal<SomeCommand>(m => { })))
                    .Done(c => c.WasInMemoryEventReceivedByHandler1 && c.WasInMemoryEventReceivedByHandler2)
                    .Run();

            Assert.True(context.WasInMemoryEventReceivedByHandler1, "class MyEventHandler1 did not receive the in-memory event");
            Assert.True(context.WasInMemoryEventReceivedByHandler2, "class MyEventHandler2 did not receive the in-memory event");
        }

        public class Context : ScenarioContext
        {
            public bool WasInMemoryEventReceivedByHandler1 { get; set; }
            public bool WasInMemoryEventReceivedByHandler2 { get; set; }

        }

        public class InMemoryEndpoint : EndpointConfigurationBuilder
        {
            public InMemoryEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CommandHandler : IHandleMessages<SomeCommand>
            {
                public IBus Bus { get; set; }
                public void Handle(SomeCommand messageThatIsEnlisted)
                {
                    Bus.InMemory.Raise<MyInMemoryEvent>(m => { });
                }
            }

            public class MyEventHandler1 : IHandleMessages<MyInMemoryEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyInMemoryEvent messageThatIsEnlisted)
                {
                    Context.WasInMemoryEventReceivedByHandler1 = true;
                }
            }

            public class MyEventHandler2 : IHandleMessages<MyInMemoryEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyInMemoryEvent messageThatIsEnlisted)
                {
                    Context.WasInMemoryEventReceivedByHandler2 = true;
                }
            }
        }


        [Serializable]
        public class SomeCommand : ICommand
        {
        }

        [Serializable]
        public class MyInMemoryEvent : IEvent
        {
        }
    }
}
