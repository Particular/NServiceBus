namespace NServiceBus.AcceptanceTests.Mutators
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_defining_outgoing_message_mutators : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_applied_to_outgoing_messages()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<OutgoingMutatorEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeMutated())))
                    .Done(c => c.MessageProcessed)
                    .Run();

            Assert.True(context.TransportMutatorCalled);
            Assert.True(context.MessageMutatorCalled);
        }

        public class Context : ScenarioContext
        {
            public bool MessageProcessed { get; set; }
            public bool TransportMutatorCalled { get; set; }
            public bool MessageMutatorCalled { get; set; }
        }

        public class OutgoingMutatorEndpoint : EndpointConfigurationBuilder
        {
            public OutgoingMutatorEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }


            class MyTransportMessageMutator : IMutateOutgoingPhysicalContext, INeedInitialization
            {

                public Context Context { get; set; }

                public void MutateOutgoing(OutgoingPhysicalMutatorContext context)
                {
                    Context.TransportMutatorCalled = true;
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<MyTransportMessageMutator>(DependencyLifecycle.InstancePerCall));
                }
            }

            class MyMessageMutator : IMutateOutgoingMessages, INeedInitialization
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public object MutateOutgoing(object message)
                {
                    Context.MessageMutatorCalled = true;

                    return message;
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<MyMessageMutator>(DependencyLifecycle.InstancePerCall));
                }

            }

            class MessageToBeMutatedHandler : IHandleMessages<MessageToBeMutated>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }


                public void Handle(MessageToBeMutated message)
                {
                    Context.MessageProcessed = true;
                }
            }

        }

        [Serializable]
        public class MessageToBeMutated : ICommand
        {
        }
    }
}