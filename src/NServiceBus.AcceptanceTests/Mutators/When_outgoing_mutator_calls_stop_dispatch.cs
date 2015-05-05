namespace NServiceBus.AcceptanceTests.Mutators
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_outgoing_mutator_calls_stop_dispatch : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_work_without_incoming_message()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new Request())))
                    .Done(c => c.Done)
                    .Run();

            Assert.IsTrue(context.Done);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(
                    b => b.RegisterComponents(r => r.ConfigureComponent<MutateOutgoingMessages>(DependencyLifecycle.InstancePerCall)));
            }

            class MutateOutgoingMessages : IMutateOutgoingMessages
            {
                readonly IBus bus;

                public MutateOutgoingMessages(IBus bus)
                {
                    this.bus = bus;
                }

                public object MutateOutgoing(object message)
                {
                    bus.DoNotContinueDispatchingCurrentMessageToHandlers();
                    bus.HandleCurrentMessageLater();

                    return message;
                }
            }

            class RequestHandler : IHandleMessages<Request>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(Request message)
                {
                    Context.Done = true;
                }
            }
        }

        [Serializable]
        public class Request : ICommand
        {
        }
    }
}