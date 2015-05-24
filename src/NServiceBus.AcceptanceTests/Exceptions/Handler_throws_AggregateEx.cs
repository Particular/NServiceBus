namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.CompilerServices;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class Handler_throws_AggregateEx : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exact_AggregateException_exception_from_handler()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                .Done(c => c.ExceptionReceived)
                .Run();
            Assert.AreEqual(typeof(AggregateException), context.ExceptionType);
            Assert.AreEqual(typeof(Exception), context.InnerExceptionType);
            Assert.AreEqual("My Exception", context.ExceptionMessage);
            Assert.AreEqual("My Inner Exception", context.InnerExceptionMessage);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public Type InnerExceptionType { get; set; }
            public string ExceptionMessage { get; set; }
            public string InnerExceptionMessage { get; set; }
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
                        Context.ExceptionMessage = e.Exception.Message;
                        Context.ExceptionType = e.Exception.GetType();
                        if (e.Exception.InnerException != null)
                        {
                            Context.InnerExceptionMessage = e.Exception.InnerException.Message;
                            Context.InnerExceptionType = e.Exception.InnerException.GetType();
                        }
                        Context.ExceptionReceived = true;
                    });
                }

                public void Stop() { }
            }


            class Handler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                    try
                    {
                        MethodThatThrows();
                    }
                    catch (Exception exception)
                    {
                        throw new AggregateException("My Exception", exception);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                void MethodThatThrows()
                {
                    throw new Exception("My Inner Exception");
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }
}