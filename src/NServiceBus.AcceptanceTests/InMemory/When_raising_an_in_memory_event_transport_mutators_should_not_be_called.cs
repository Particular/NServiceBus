namespace NServiceBus.AcceptanceTests.InMemory
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using MessageMutator;
    using NUnit.Framework;

#pragma warning disable 612, 618
    public class When_raising_an_in_memory_event_transport_mutators_should_not_be_called : NServiceBusAcceptanceTest
    {
        [Test]
        public void Ensure_transport_mutators_should_not_be_called()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<InMemoryEndpoint>(b => b.Given((bus, c) => bus.InMemory.Raise<MyInMemoryEvent>(m => { })))
                .Run();

            Assert.False(context.MutateIncomingCalled);
            Assert.False(context.MutateOutGoingCalled);
        }

        class Mutator : IMutateTransportMessages, INeedInitialization
        {
            public Context Context { get; set; }

            public void MutateIncoming(TransportMessage transportMessage)
            {
                Context.MutateIncomingCalled = true;
            }

            public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
            {
                Context.MutateOutGoingCalled = true;
            }

            public void Init()
            {
                Configure.Component<Mutator>(DependencyLifecycle.InstancePerCall);
            }
        }

        public class Context : ScenarioContext
        {
            public bool MutateOutGoingCalled { get; set; }
            public bool MutateIncomingCalled { get; set; }
        }

        public class InMemoryEndpoint : EndpointConfigurationBuilder
        {
            public InMemoryEndpoint()
            {
                EndpointSetup<DefaultServer>(c => Configure.Features.Disable<AutoSubscribe>());
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
#pragma warning restore  612, 618
}
