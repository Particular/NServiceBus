namespace NServiceBus.AcceptanceTests.Mutators
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_outgoing_mutator_replaces_instance : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_sent_should_be_new_instance()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocal(new V1Message())))
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
                public Task MutateOutgoing(MutateOutgoingMessageContext context)
                {
                    if (context.OutgoingMessage is V1Message)
                    {
                        context.OutgoingMessage = new V2Message();
                    }
                    return Task.FromResult(0);
                }
            }

            class V2Handler : IHandleMessages<V2Message>
            {
                Context testContext;
                public V2Handler(Context testContext)
                {
                    this.testContext = testContext;
                }
                public Task Handle(V2Message message, IMessageHandlerContext context)
                {
                    testContext.V2MessageReceived = true;

                    return Task.FromResult(0);
                }
            }

            class V1Handler : IHandleMessages<V1Message>
            {
                Context testContext;
                public V1Handler(Context testContext)
                {
                    this.testContext = testContext;
                }
                public Task Handle(V1Message message, IMessageHandlerContext context)
                {
                    testContext.V1MessageReceived = true;

                    return Task.FromResult(0);
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