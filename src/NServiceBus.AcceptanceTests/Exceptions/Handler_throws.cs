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

            StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.Handler_throws.Endpoint.Handler.Handle(Message message)
at NServiceBus.Unicast.MessageHandlerRegistry.Invoke(Object handler, Object message, Dictionary`2 dictionary)
at NServiceBus.InvokeHandlersBehavior.Invoke(Context context, Action next)
at NServiceBus.HandlerTransactionScopeWrapperBehavior.Invoke(Context context, Action next)
at NServiceBus.LoadHandlersConnector.Invoke(Context context, Action`1 next)
at NServiceBus.ApplyIncomingMessageMutatorsBehavior.Invoke(Context context, Action next)
at NServiceBus.ExecuteLogicalMessagesConnector.Invoke(Context context, Action`1 next)
at NServiceBus.CallbackInvocationBehavior.Invoke(Context context, Action next)
at NServiceBus.ApplyIncomingTransportMessageMutatorsBehavior.Invoke(Context context, Action next)
at NServiceBus.SubscriptionReceiverBehavior.Invoke(Context context, Action next)
at NServiceBus.UnitOfWorkBehavior.Invoke(Context context, Action next)
at NServiceBus.ChildContainerBehavior.Invoke(Context context, Action next)
at NServiceBus.ProcessingStatisticsBehavior.Invoke(Context context, Action next)
at NServiceBus.EnforceMessageIdBehavior.Invoke(Context context, Action next)
at NServiceBus.HostInformationBehavior.Invoke(Context context, Action next)
at NServiceBus.MoveFaultsToErrorQueueBehavior.Invoke(Context context, Action next)", context.StackTrace);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string StackTrace { get; set; }
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
                        Context.StackTrace = e.Exception.StackTrace;
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
