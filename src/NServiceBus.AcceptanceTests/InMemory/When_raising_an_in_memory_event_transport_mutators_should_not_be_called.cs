namespace NServiceBus.AcceptanceTests.InMemory
{
    using System;
    using Audit;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NUnit.Framework;

    public class When_raising_an_in_memory_event_transport_mutators_should_not_be_called : NServiceBusAcceptanceTest
    {
        [Test]
        public void Ensure_transport_mutators_should_not_be_called()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<InMemoryEndpoint>(b => b.Given((bus, c) => bus.InMemory.Raise<MyInMemoryEvent>(m => { })))
                .Run();

            Assert.False(context.MutateIncomingWasCalledInMutator);
            Assert.False(context.MutateOutGoingWasCalledInMutator);
        }

        class Mutator : IMutateTransportMessages, INeedInitialization
        {
            public Context Context { get; set; }

            public void MutateIncoming(TransportMessage transportMessage)
            {
                Context.MutateIncomingWasCalledInMutator = true;
            }

            public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
            {
                Context.MutateOutGoingWasCalledInMutator = true;
            }

            public void Init()
            {
                Configure.Component<Mutator>(DependencyLifecycle.InstancePerCall);
            }
        }

        public class Context : ScenarioContext
        {
            public bool MutateOutGoingWasCalledInMutator { get; set; }
            public bool MutateIncomingWasCalledInMutator { get; set; }
        }

        public class InMemoryEndpoint : EndpointConfigurationBuilder
        {
            public InMemoryEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

        }

        public class MyEventHandler : IHandleMessages<MyInMemoryEvent>
        {
            public Context Context { get; set; }

            public void Handle(MyInMemoryEvent messageThatIsEnlisted)
            {
            }
        }


        [Serializable]
        public class MyInMemoryEvent : IEvent
        {
        }
    }
}
