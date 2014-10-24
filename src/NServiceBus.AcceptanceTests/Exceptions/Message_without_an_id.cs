namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.MessageMutator;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    public class Message_without_an_id : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_invoke_start_message_processing_listeners()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.StartMessageProcessingCalled)
                    .Run();

            Assert.IsTrue(context.StartMessageProcessingCalled);
        }

        public class Context : ScenarioContext
        {
            public bool StartMessageProcessingCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class StartProcessingListener : IWantToRunWhenBusStartsAndStops
            {
                readonly UnicastBus bus;
                Context context;

                public StartProcessingListener(UnicastBus bus, Context context)
                {
                    this.bus = bus;
                    this.context = context;
                    bus.Transport.StartedMessageProcessing += transport_StartedMessageProcessing;
                }

                void transport_StartedMessageProcessing(object sender, StartedMessageProcessingEventArgs e)
                {
                    context.StartMessageProcessingCalled = true;
                }

                public void Start()
                {
                    bus.SendLocal(new Message());
                }

                public void Stop()
                {
                    bus.Transport.StartedMessageProcessing -= transport_StartedMessageProcessing;
                }
            }

            class CorruptionMutator : IMutateOutgoingTransportMessages
            {
                public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
                {
                    transportMessage.Headers[Headers.MessageId] = "";
                }
            }

            class Handler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }
    
}