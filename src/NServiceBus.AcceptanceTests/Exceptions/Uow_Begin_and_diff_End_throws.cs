﻿namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class Uow_Begin_and_diff_End_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_AggregateException_with_both_exceptions()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(BeginException), context.InnerExceptionOneType);
            Assert.AreEqual(typeof(EndException), context.InnerExceptionTwoType);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public Type ExceptionType { get; set; }
            public Type InnerExceptionOneType { get; set; }
            public Type InnerExceptionTwoType { get; set; }
            public bool FirstOneExecuted { get; set; }
            public string TypeName { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.RegisterComponents(c =>
                    {
                        c.ConfigureComponent<UnitOfWorkThatThrows1>(DependencyLifecycle.InstancePerUnitOfWork);
                        c.ConfigureComponent<UnitOfWorkThatThrows2>(DependencyLifecycle.InstancePerUnitOfWork);
                    });
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
                        var aggregateException = (AggregateException)e.Exception;
                        var innerExceptions = aggregateException.InnerExceptions;
                        Context.InnerExceptionOneType = innerExceptions[0].GetType();
                        Context.InnerExceptionTwoType = innerExceptions[1].GetType();
                        Context.ExceptionReceived = true;
                    });
                }

                public void Stop() { }
            }

            public class UnitOfWorkThatThrows1 : IManageUnitsOfWork
            {
                public Context Context { get; set; }
                
                bool throwAtEnd;

                public void Begin()
                {
                    if (Context.FirstOneExecuted)
                    {
                        throw new BeginException();
                    }

                    Context.FirstOneExecuted = throwAtEnd = true;
                }

                public void End(Exception ex = null)
                {
                    if (throwAtEnd)
                    {
                        Context.TypeName = GetType().Name;

                        throw new EndException();
                    }
                }
            }
            public class UnitOfWorkThatThrows2 : IManageUnitsOfWork
            {
                public Context Context { get; set; }

                bool throwAtEnd;

                public void Begin()
                {
                    if (Context.FirstOneExecuted)
                    {
                        throw new BeginException();
                    }

                    Context.FirstOneExecuted = throwAtEnd = true;
                }

                public void End(Exception ex = null)
                {
                    if (throwAtEnd)
                    {
                        Context.TypeName = GetType().Name;

                        throw new EndException();
                    }
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

        [Serializable]
        public class BeginException : Exception
        {
            public BeginException()
                : base("BeginException")
            {

            }

            protected BeginException(SerializationInfo info, StreamingContext context)
            {
            }
        }

        [Serializable]
        public class EndException : Exception
        {
            public EndException()
                : base("EndException")
            {

            }

            protected EndException(SerializationInfo info, StreamingContext context)
            {
            }
        }
    }

}