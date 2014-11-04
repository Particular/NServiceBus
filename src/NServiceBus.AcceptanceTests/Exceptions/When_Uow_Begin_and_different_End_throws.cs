namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NServiceBus.UnitOfWork;
    using NUnit.Framework;

    public class When_Uow_Begin_and_different_End_throws : NServiceBusAcceptanceTest
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

            StackTraceAssert.StartsWith(
@"at NServiceBus.UnitOfWorkBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ChildContainerBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ProcessingStatisticsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Pipeline.PipelineExecutor.Execute[T](BehaviorChain`1 pipelineAction, T context)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)", context.StackTrace);

            StackTraceAssert.StartsWith(
string.Format(@"at NServiceBus.AcceptanceTests.Exceptions.When_Uow_Begin_and_different_End_throws.Endpoint.{0}.End(Exception ex)
at NServiceBus.UnitOfWorkBehavior.AppendEndExceptionsAndRethrow(Exception initialException)", context.TypeName), context.InnerExceptionTwoStackTrace);

        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string StackTrace { get; set; }
            public Type ExceptionType { get; set; }
            public string InnerExceptionOneStackTrace { get; set; }
            public string InnerExceptionTwoStackTrace { get; set; }
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
                        c.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance);
                        c.ConfigureComponent<UnitOfWorkThatThrows1>(DependencyLifecycle.InstancePerUnitOfWork);
                        c.ConfigureComponent<UnitOfWorkThatThrows2>(DependencyLifecycle.InstancePerUnitOfWork);
                    });
                    b.DisableFeature<TimeoutManager>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class CustomFaultManager : IManageMessageFailures
            {
                public Context Context { get; set; }

                public void SerializationFailedForMessage(TransportMessage message, Exception e)
                {
                }

                public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
                {
                    var aggregateException = (AggregateException)e;
                    Context.StackTrace = aggregateException.StackTrace;
                    var innerExceptions = aggregateException.InnerExceptions;
                    Context.InnerExceptionOneStackTrace = innerExceptions[0].StackTrace;
                    Context.InnerExceptionTwoStackTrace = innerExceptions[1].StackTrace;
                    Context.InnerExceptionOneType = innerExceptions[0].GetType();
                    Context.InnerExceptionTwoType = innerExceptions[1].GetType();
                    Context.ExceptionReceived = true;
                }

                public void Init(Address address)
                {

                }
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
        public class BeginException : Exception
        {
            public BeginException()
                : base("BeginException")
            {

            }
        }
        public class EndException : Exception
        {
            public EndException()
                : base("EndException")
            {

            }
        }
    }

}