namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.CompilerServices;
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

#if (!DEBUG)
            StackTraceAssert.AreEqual(
@"at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Behaviors.ChildContainerBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ProcessingStatisticsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Pipeline.PipelineExecutor.Execute[T](BehaviorChain`1 pipelineAction, T context)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)
at NServiceBus.Transports.Msmq.MsmqDequeueStrategy.Action()", context.StackTrace);

            StackTraceAssert.AreEqual(
@"at NServiceBus.AcceptanceTests.ManageFailures.When_handler_and_Uow_End_throws.Endpoint.Handler.Handle(Message message)
at NServiceBus.Unicast.MessageHandlerRegistry.Invoke(Object handler, Object message, Dictionary`2 dictionary)
at NServiceBus.Unicast.Behaviors.InvokeHandlersBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Behaviors.SetCurrentMessageBeingHandledBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Behaviors.LoadHandlersBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Pipeline.MessageMutator.ApplyIncomingMessageMutatorsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Messages.ExecuteLogicalMessagesBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Messages.DeserializeLogicalMessagesBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Behaviors.CallbackInvocationBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.MessageMutator.ApplyIncomingTransportMessageMutatorsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions.SubscriptionReceiverBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(IncomingContext context, Action next)", context.InnerExceptionOneStackTrace);

            StackTraceAssert.AreEqual(
@"at NServiceBus.AcceptanceTests.ManageFailures.When_handler_and_Uow_End_throws.Endpoint.UnitOfWorkThatThrowsInEnd.End(Exception ex)
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.AppendEndExceptionsAndRethrow(Exception initialException)", context.InnerExceptionTwoStackTrace);
            
#endif
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string StackTrace { get; set; }
            public string InnerExceptionOneStackTrace { get; set; }
            public string InnerExceptionTwoStackTrace { get; set; }
            public Type InnerExceptionOneType { get; set; }
            public Type InnerExceptionTwoType { get; set; }
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
                        c.ConfigureComponent<UnitOfWorkThatThrowsInEnd>(DependencyLifecycle.InstancePerUnitOfWork);
                        c.ConfigureComponent<UnitOfWorkThatThrowsInBegin>(DependencyLifecycle.InstancePerUnitOfWork);
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

            public class UnitOfWorkThatThrowsInBegin : IManageUnitsOfWork
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Begin()
                {
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void End(Exception ex = null)
                {
                }
            }
            public class UnitOfWorkThatThrowsInEnd : IManageUnitsOfWork
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Begin()
                {
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void End(Exception ex = null)
                {
                    throw new EndException();
                }
            }
            class Handler : IHandleMessages<Message>
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
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