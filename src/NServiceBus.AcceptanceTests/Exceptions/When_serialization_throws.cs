namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.MessageMutator;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;

    public class When_serialization_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_MessageDeserializationException()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(MessageDeserializationException), context.ExceptionType);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public Type ExceptionType { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.RegisterComponents(c => c.ConfigureComponent<CorruptionMutator>(DependencyLifecycle.InstancePerCall));
                    b.DisableFeature<TimeoutManager>();
                    b.DisableFeature<SecondLevelRetries>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.ExceptionType = e.Exception.GetType();
                        Context.ExceptionReceived = true;
                    });
                }

                public void Stop() { }
            }


            class CorruptionMutator : IMutateTransportMessages
            {
                public void MutateIncoming(TransportMessage transportMessage)
                {
                    transportMessage.Body[1]++;
                }

                public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
                {
                }
            }

            class Handler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                }
            }
        }

        public class Message : IMessage
        {
        }
    }

}