namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.CompilerServices;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_handler_throws_AggregateException : NServiceBusAcceptanceTest
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
      
            StackTraceAssert.StartsWith(
                @"at NServiceBus.AcceptanceTests.Exceptions.When_handler_throws_AggregateException.Endpoint.Handler.Handle(Message message)
at NServiceBus.Unicast.MessageHandlerRegistry.Invoke(Object handler, Object message, Dictionary`2 dictionary)
at NServiceBus.InvokeHandlersBehavior.Invoke(Context context, Action next)
at NServiceBus.LoadHandlersConnector.Invoke(Context context, Action`1 next)
at NServiceBus.ApplyIncomingMessageMutatorsBehavior.Invoke(Context context, Action next)
at NServiceBus.ExecuteLogicalMessagesConnector.Invoke(Context context, Action`1 next)
at NServiceBus.CallbackInvocationBehavior.Invoke(Context context, Action next)
at NServiceBus.DeserializeLogicalMessagesConnector.Invoke(Context context, Action`1 next)
at NServiceBus.ApplyIncomingTransportMessageMutatorsBehavior.Invoke(Context context, Action next)
at NServiceBus.SubscriptionReceiverBehavior.Invoke(Context context, Action next)
at NServiceBus.UnitOfWorkBehavior.Invoke(Context context, Action next)
at NServiceBus.ChildContainerBehavior.Invoke(Context context, Action next)
at NServiceBus.ProcessingStatisticsBehavior.Invoke(Context context, Action next)", context.StackTrace);

            StackTraceAssert.StartsWith(
                @"at NServiceBus.AcceptanceTests.Exceptions.When_handler_throws_AggregateException.Endpoint.Handler.MethodThatThrows()
at NServiceBus.AcceptanceTests.Exceptions.When_handler_throws_AggregateException.Endpoint.Handler.Handle(Message message)", context.InnerStackTrace);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string StackTrace { get; set; }
            public string InnerStackTrace { get; set; }
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
                        Context.StackTrace = e.Exception.StackTrace;
                        Context.ExceptionType = e.Exception.GetType();
                        if (e.Exception.InnerException != null)
                        {
                            Context.InnerExceptionMessage = e.Exception.InnerException.Message;
                            Context.InnerExceptionType = e.Exception.InnerException.GetType();
                            Context.InnerStackTrace = e.Exception.InnerException.StackTrace;
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