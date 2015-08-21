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
                .WithEndpoint<Endpoint>(b => b.When(c => c.Subscribed, bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                .Done(c => c.ExceptionReceived)
                .Run();

            Assert.AreEqual(typeof(AggregateException), context.Exception.GetType());
            Assert.IsNotNull(context.Exception.InnerException);
            Assert.AreEqual(typeof(Exception), context.Exception.InnerException.GetType());
            Assert.AreEqual("My Exception", context.Exception.Message);
            Assert.AreEqual("My Inner Exception", context.Exception.InnerException.Message);

            StackTraceAssert.StartsWith(
                @"at NServiceBus.AcceptanceTests.Exceptions.Handler_throws_AggregateEx.Endpoint.Handler.Handle(Message message)", context.Exception);

            StackTraceAssert.StartsWith(
                @"at NServiceBus.AcceptanceTests.Exceptions.Handler_throws_AggregateEx.Endpoint.Handler.MethodThatThrows()", context.Exception.InnerException);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public Exception Exception { get; set; }
            public bool Subscribed { get; set; }
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
                        Context.Exception = e.Exception;
                        Context.ExceptionReceived = true;
                    });

                    Context.Subscribed = true;
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