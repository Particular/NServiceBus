namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class Handler_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exception_from_handler()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();
            Assert.AreEqual(typeof(HandlerException), context.ExceptionType);
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

            class Handler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                    throw new HandlerException();
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }

        [Serializable]
        public class HandlerException : Exception
        {
            public HandlerException()
                : base("HandlerException")
            {

            }

            protected HandlerException(SerializationInfo info, StreamingContext context)
            {
            }
        }
    }
    
}
