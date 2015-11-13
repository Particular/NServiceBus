namespace NServiceBus.AcceptanceTests.Mutators
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class Issue_1980 : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Run()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocal(new V1Message())))
                    .Done(c => c.V2MessageReceived || c.V1MessageReceived)
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
                    b => b.RegisterComponents(r => r.ConfigureComponent<MutateIncomingMessages>(DependencyLifecycle.InstancePerCall)));
            }

            class MutateIncomingMessages : IMutateIncomingMessages
            {
                public Task MutateIncoming(MutateIncomingMessageContext message)
                {
                    if (message.Message is V1Message)
                    {
                        message.Message=new V2Message();
                    }
                    return Task.FromResult(0);
                }
            }

            class V2MessageHandler : IHandleMessages<V2Message>
            {
                Context testContext;
                public V2MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(V2Message message, IMessageHandlerContext context)
                {
                    testContext.V2MessageReceived = true;
                    return Task.FromResult(0);
                }
            }

            class V1MessageHandler : IHandleMessages<V1Message>
            {
                Context testContext;
                public V1MessageHandler(Context testContext)
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