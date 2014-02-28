namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NUnit.Framework;

    public class When_defining_outoing_message_mutators : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_applied_to_outgoing_messages()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<OutgoingMutatatorEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeMutated())))
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

        public class OutgoingMutatatorEndpoint : EndpointConfigurationBuilder
        {
            public OutgoingMutatatorEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }


            class MyTransportMessageMutator:IMutateOutgoingTransportMessages,INeedInitialization
            {
                public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
                {
                    transportMessage.Headers["TransportMutatorCalled"] = true.ToString();
                }

                public void Init()
                {
                    Configure.Component<MyTransportMessageMutator>(DependencyLifecycle.InstancePerCall);
                }
            }

            class MyMessageMutator : IMutateOutgoingMessages, INeedInitialization
            {

             
                public object MutateOutgoing(object message)
                {
                    Headers.SetMessageHeader(message,"MessageMutatorCalled","true");

                    return message;
                }

                public void Init()
                {
                    Configure.Component<MyMessageMutator>(DependencyLifecycle.InstancePerCall);
                }

            }

            class MessageToBeMutatedHandler : IHandleMessages<MessageToBeMutated>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }


                public void Handle(MessageToBeMutated message)
                {
                    Context.TransportMutatorCalled = Bus.CurrentMessageContext.Headers.ContainsKey("TransportMutatorCalled");
                    Context.MessageMutatorCalled = Bus.CurrentMessageContext.Headers.ContainsKey("MessageMutatorCalled");

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