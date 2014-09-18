namespace NServiceBus.AcceptanceTests.Mutators
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_outgoing_mutator_replaces_instance : NServiceBusAcceptanceTest
    {
        [Test]
        public void Message_sent_should_be_new_instance()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new V1Message())))
                .Done(c => c.V2MessageReceived)
                .Run();

            Assert.IsTrue(context.V2MessageReceived);
            Assert.IsFalse(context.V1MessageReceived);
        }

        public class Context : ScenarioContext
        {
            public bool V1MessageReceived { get; set; }
            public bool V2MessageReceived { get; set; }
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
                public object MutateOutgoing(object message)
                {
                    if (message is V1Message)
                    {
                        return new V2Message();
                    }

                    return message;
                }
            }

            class V2MessageHandler : IHandleMessages<V2Message>
            {
                public Context Context { get; set; }

                public void Handle(V2Message message)
                {
                    Context.V2MessageReceived = true;
                }
            }

            class V1MessageHandler : IHandleMessages<V1Message>
            {
                public Context Context { get; set; }

                public void Handle(V1Message message)
                {
                    Context.V1MessageReceived = true;
                }
            }
        }

        [Serializable]
        public class V1Message : ICommand
        {
        }

        [Serializable]
        public class V2Message : ICommand
        {
        }
    }
}