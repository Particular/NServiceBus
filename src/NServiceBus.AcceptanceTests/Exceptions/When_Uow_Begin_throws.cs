namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class When_Uow_Begin_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exception_thrown_from_begin()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(BeginException), context.ExceptionType);
            StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.When_Uow_Begin_throws.Endpoint.UnitOfWorkThatThrowsInBegin.Begin()
at NServiceBus.UnitOfWorkBehavior.Invoke(Context context, Action next)
at NServiceBus.ChildContainerBehavior.Invoke(Context context, Action next)
at NServiceBus.ProcessingStatisticsBehavior.Invoke(Context context, Action next)", context.StackTrace);
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
                    b.RegisterComponents(c => c.ConfigureComponent<UnitOfWorkThatThrowsInBegin>(DependencyLifecycle.InstancePerUnitOfWork));
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

            public class UnitOfWorkThatThrowsInBegin : IManageUnitsOfWork
            {
                public void Begin()
                {
                    throw new BeginException();
                }

                public void End(Exception ex = null)
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

        [Serializable]
        public class Message : IMessage
        {
        }
        public class BeginException : Exception
        {
            public BeginException()
                : base("BeginException")
            {

            }
        }
    }

}