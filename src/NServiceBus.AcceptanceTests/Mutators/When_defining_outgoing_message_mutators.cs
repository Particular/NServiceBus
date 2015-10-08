﻿namespace NServiceBus.AcceptanceTests.Mutators
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_defining_outgoing_message_mutators : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_applied_to_outgoing_messages()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocalAsync(new Message())))
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

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.RegisterComponents(
                    components =>
                    {
                        components.ConfigureComponent<TransportMutator>(DependencyLifecycle.InstancePerCall);
                        components.ConfigureComponent<MessageMutator>(DependencyLifecycle.InstancePerCall);
                    }));
            }

            class TransportMutator :
                IMutateOutgoingTransportMessages
            {

                Context testContext;
                public TransportMutator(Context testContext)
                {
                    this.testContext = testContext;
                }
                public void MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    testContext.TransportMutatorCalled = true;
                }

            }

            class MessageMutator : IMutateOutgoingMessages
            {
                Context testContext;
                public MessageMutator(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task MutateOutgoing(MutateOutgoingMessageContext context)
                {
                    testContext.MessageMutatorCalled = true;
                    return Task.FromResult(0);
                }
            }

            class Handler : IHandleMessages<Message>
            {
                Context testContext;
                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Message message)
                {
                    testContext.MessageProcessed = true;

                    return Task.FromResult(0);
                }
            }

        }

        [Serializable]
        public class Message : ICommand
        {
        }
    }
}