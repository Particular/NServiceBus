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

    public class Uow_End_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exception_thrown_from_end()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b =>
                    {
                        b.Given(bus => bus.SendLocal(new Message()));
                        b.CustomConfig(c => c.Notifications(n => n.Errors.MessageSentToErrorQueue.Subscribe(e =>
                        {
                            context.ExceptionType = e.Exception.GetType();
                            context.StackTrace = e.Exception.StackTrace;
                            context.ExceptionReceived = true;
                        })));
                    })
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(EndException), context.ExceptionType);
            StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.Uow_End_throws.Endpoint.UnitOfWorkThatThrowsInEnd.End(Exception ex)
at NServiceBus.UnitOfWorkBehavior.Invoke(Context context, Action next)
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
                    b.RegisterComponents(c => c.ConfigureComponent<UnitOfWorkThatThrowsInEnd>(DependencyLifecycle.InstancePerUnitOfWork));
                    b.DisableFeature<TimeoutManager>();
                    b.DisableFeature<SecondLevelRetries>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class UnitOfWorkThatThrowsInEnd : IManageUnitsOfWork
            {
                public void Begin()
                {
                }

                public void End(Exception ex = null)
                {
                    throw new EndException();
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