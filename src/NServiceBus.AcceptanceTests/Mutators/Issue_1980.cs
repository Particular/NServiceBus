namespace NServiceBus.AcceptanceTests.Mutators
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class Issue_1980 : NServiceBusAcceptanceTest
    {
        static Context testContext = new Context();
        [Test]
        public void Run()
        {
            Scenario.Define(testContext)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new V1Message())))
                    .Done(c => c.V2MessageReceived || c.V1MessageReceived)
                    .Run();

            Assert.IsTrue(testContext.V2MessageReceived);
            Assert.IsFalse(testContext.V1MessageReceived);
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
                public void MutateIncoming(MutateIncomingMessageContext message)
                {
                    if (message.Message is V1Message)
                    {
                        message.Message=new V2Message();
                    }
                }
            }

            class V2MessageHandler : IHandleMessages<V2Message>
            {
                public void Handle(V2Message message)
                {
                    testContext.V2MessageReceived = true;
                }
            }

            class V1MessageHandler : IHandleMessages<V1Message>
            {
                public void Handle(V1Message message)
                {
                    testContext.V1MessageReceived = true;
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