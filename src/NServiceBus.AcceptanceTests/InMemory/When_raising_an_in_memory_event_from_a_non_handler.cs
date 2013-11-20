namespace NServiceBus.AcceptanceTests.InMemory
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_raising_an_in_memory_event_from_a_non_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_delivered_to_handlers()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<InMemoryEndpoint>()
                    .Done(c => c.WasInMemoryEventReceivedByHandler1)
                    .Run();

            Assert.True(context.WasInMemoryEventReceivedByHandler1, "class MyEventHandler1 did not receive the in-memory event");
        }

        public class Context : ScenarioContext
        {
            public bool WasInMemoryEventReceivedByHandler1 { get; set; }
      
        }

        public class InMemoryEndpoint : EndpointConfigurationBuilder
        {
            public InMemoryEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class StartUpRunner : IWantToRunWhenBusStartsAndStops
            {
                public IBus Bus { get; set; }
              
                public void Start()
                {
                    Bus.InMemory.Raise<MyInMemoryEvent>(m =>
                    {
                        m.SetHeader("MyHeader","MyValue");
                    });      
                }

                public void Stop()
                {
                }
            }

            public class MyEventHandler1 : IHandleMessages<MyInMemoryEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyInMemoryEvent message)
                {
                    Assert.AreEqual("MyValue",Headers.GetMessageHeader(message,"MyHeader"));
                    Context.WasInMemoryEventReceivedByHandler1 = true;
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
