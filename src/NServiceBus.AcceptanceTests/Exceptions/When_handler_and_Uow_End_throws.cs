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

    public class When_handler_and_Uow_End_throws : NServiceBusAcceptanceTest
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

            Assert.AreEqual(typeof(HandlerException), context.InnerExceptionOneType);
            Assert.AreEqual(typeof(EndException), context.InnerExceptionTwoType);

            StackTraceAssert.StartsWith(
@"at NServiceBus.UnitOfWorkBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ChildContainerBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ProcessingStatisticsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Pipeline.PipelineExecutor.Execute[T](BehaviorChain`1 pipelineAction, T context)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)", context.StackTrace);

            StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.When_handler_and_Uow_End_throws.Endpoint.Handler.Handle(Message message)
at NServiceBus.Unicast.MessageHandlerRegistry.Invoke(Object handler, Object message, Dictionary`2 dictionary)
at NServiceBus.InvokeHandlersBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.SetCurrentMessageBeingHandledBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.LoadHandlersBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ApplyIncomingMessageMutatorsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ExecuteLogicalMessagesBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.CallbackInvocationBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.DeserializeLogicalMessagesBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ApplyIncomingTransportMessageMutatorsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.SubscriptionReceiverBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.UnitOfWorkBehavior.Invoke(IncomingContext context, Action next)", context.InnerExceptionOneStackTrace);

            StackTraceAssert.StartsWith(
string.Format(@"at NServiceBus.AcceptanceTests.Exceptions.When_handler_and_Uow_End_throws.Endpoint.{0}.End(Exception ex)
at NServiceBus.UnitOfWorkBehavior.AppendEndExceptionsAndRethrow(Exception initialException)", context.TypeName), context.InnerExceptionTwoStackTrace);
            
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string StackTrace { get; set; }
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
                        c.ConfigureComponent<UnitOfWorkThatThrows2>(DependencyLifecycle.InstancePerUnitOfWork);
                        c.ConfigureComponent<UnitOfWorkThatThrows1>(DependencyLifecycle.InstancePerUnitOfWork);
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
                    var exceptions = aggregateException.InnerExceptions;
                    Context.InnerExceptionOneStackTrace = exceptions[0].StackTrace;
                    Context.InnerExceptionTwoStackTrace = exceptions[1].StackTrace;
                    Context.InnerExceptionOneType = exceptions[0].GetType();
                    Context.InnerExceptionTwoType = exceptions[1].GetType();
                    Context.ExceptionReceived = true;
                }

                public void Init(Address address)
                {

                }
            }

            public class UnitOfWorkThatThrows1 : IManageUnitsOfWork
            {
                public Context Context { get; set; }

                bool executedInSecondPlace;

                public void Begin()
                {
                    if (Context.FirstOneExecuted)
                    {
                        executedInSecondPlace = true;
                    }

                    Context.FirstOneExecuted = true;
                }

                public void End(Exception ex = null)
                {
                    if (executedInSecondPlace)
                    {
                        Context.TypeName = GetType().Name;

                        throw new EndException();
                    }
                }
            }

            public class UnitOfWorkThatThrows2 : IManageUnitsOfWork
            {
                public Context Context { get; set; }

                bool executedInSecondPlace;

                public void Begin()
                {
                    if (Context.FirstOneExecuted)
                    {
                        executedInSecondPlace = true;
                    }

                    Context.FirstOneExecuted = true;
                }

                public void End(Exception ex = null)
                {
                    if (executedInSecondPlace)
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
                    throw new HandlerException();
                }
            }

        }

        [Serializable]
        public class Message : IMessage
        {
        }
        public class HandlerException : Exception
        {
            public HandlerException()
                : base("HandlerException")
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