namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.MessageMutator;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    public class When_processing_a_message_without_an_id : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_invoke_start_message_processing_listeners()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .Done(c => c.StartMessageProcessingCalled)
                    .Run();

            Assert.IsTrue(context.StartMessageProcessingCalled);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public bool StartMessageProcessingCalled { get; set; }
            public string StackTrace { get; set; }
            public Type ExceptionType { get; set; }
            public string InnerExceptionStackTrace { get; set; }
            public Type InnerExceptionType { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Configurer.ConfigureComponent<CorruptionMutator>(DependencyLifecycle.InstancePerCall);
                    c.Configurer.ConfigureComponent<StartProcessingListener>(DependencyLifecycle.SingleInstance);
                    c.DisableTimeoutManager();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    })
                    .AllowExceptions();
            }

            class StartProcessingListener : IWantToRunWhenBusStartsAndStops
            {
                readonly Context context;

                public StartProcessingListener(UnicastBus bus, Context context)
                {
                    this.context = context;
                    bus.Transport.StartedMessageProcessing += transport_StartedMessageProcessing;
                }

                void transport_StartedMessageProcessing(object sender, StartedMessageProcessingEventArgs e)
                {
                    context.StartMessageProcessingCalled = true;
                }

                public void Start()
                {
                }

                public void Stop()
                {
                }
            }

            class CorruptionMutator : IMutateTransportMessages
            {
                public void MutateIncoming(TransportMessage transportMessage)
                {
                }

                public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
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