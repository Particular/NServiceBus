namespace NServiceBus.AcceptanceTests.InMemory
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_calling_DoNotContinueDispatchingCurrentMessageToHandlers_from_inmemory : NServiceBusAcceptanceTest
    {
        [Test]
        public void Subsequent_inmemory_handlers_will_not_be_invoked()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.Given(bus => bus.SendLocal(new SomeMessage())))
                .Done(c => c.SecondHandlerInvoked)
                .Run();

            Assert.IsTrue(context.FirstHandlerInvoked);
            Assert.IsTrue(context.SecondHandlerInvoked);
            Assert.IsTrue(context.InMemoryEventReceivedByHandler1);
            Assert.IsFalse(context.InMemoryEventReceivedByHandler2);
        }

        public class Context : ScenarioContext
        {
            public bool FirstHandlerInvoked { get; set; }
            public bool SecondHandlerInvoked { get; set; }

            public bool InMemoryEventReceivedByHandler1 { get; set; }
            public bool InMemoryEventReceivedByHandler2 { get; set; }
            
        }

        [Serializable]
        public class SomeMessage : IMessage { }

        [Serializable]
        public class InMemoryEvent : IEvent
        {
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class EnsureOrdering : ISpecifyMessageHandlerOrdering
            {
                public void SpecifyOrder(Order order)
                {
                    order.Specify(First<FirstHandler>.Then<SecondHandler>().AndThen<MyEventHandler1>().AndThen<MyEventHandler2>());
                }
            }

            class FirstHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                
                public IBus Bus { get; set; }
                
                public void Handle(SomeMessage message)
                {
                    Context.FirstHandlerInvoked = true;

                    Bus.InMemory.Raise<InMemoryEvent>(m => { });

                }
            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }
                
                public void Handle(SomeMessage message)
                {
                    Context.SecondHandlerInvoked = true;
                }
            }

            class MyEventHandler1 : IHandleMessages<InMemoryEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(InMemoryEvent messageThatIsEnlisted)
                {
                    Bus.DoNotContinueDispatchingCurrentMessageToHandlers();

                    Context.InMemoryEventReceivedByHandler1 = true;
                }
            }

            class MyEventHandler2 : IHandleMessages<InMemoryEvent>
            {
                public Context Context { get; set; }

                public void Handle(InMemoryEvent messageThatIsEnlisted)
                {
                    Context.InMemoryEventReceivedByHandler2 = true;
                }
            }
        }
    }
}