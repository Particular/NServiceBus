namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.CompilerServices;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Faults;
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
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(HandlerException), context.InnerExceptionOneType);
            Assert.AreEqual(typeof(EndException), context.InnerExceptionTwoType);

#if (!DEBUG)
            StackTraceAssert.StartsWith(
@"at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ForwardBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Audit.AuditBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ImpersonateSenderBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.MessageHandlingLoggingBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ChildContainerBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)", context.StackTrace);

            StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.When_handler_and_Uow_End_throws.Endpoint.Handler.Handle(Message message)
at NServiceBus.Unicast.HandlerInvocationCache.Invoke(Object handler, Object message, Dictionary`2 dictionary)
at NServiceBus.Unicast.Behaviors.InvokeHandlersBehavior.Invoke(HandlerInvocationContext context, Action next)
at NServiceBus.Sagas.SagaPersistenceBehavior.Invoke(HandlerInvocationContext context, Action next)
at NServiceBus.Sagas.AuditInvokedSagaBehavior.Invoke(HandlerInvocationContext context, Action next)
at NServiceBus.Unicast.Behaviors.SetCurrentMessageBeingHandledBehavior.Invoke(HandlerInvocationContext context, Action next)
at NServiceBus.Pipeline.PipelineExecutor.Execute[T](BehaviorChain`1 pipelineAction, T context)
at NServiceBus.Unicast.Behaviors.LoadHandlersBehavior.Invoke(ReceiveLogicalMessageContext context, Action next)
at NServiceBus.DataBus.DataBusReceiveBehavior.Invoke(ReceiveLogicalMessageContext context, Action next)
at NServiceBus.Pipeline.MessageMutator.ApplyIncomingMessageMutatorsBehavior.Invoke(ReceiveLogicalMessageContext context, Action next)
at NServiceBus.Pipeline.PipelineExecutor.Execute[T](BehaviorChain`1 pipelineAction, T context)
at NServiceBus.Unicast.Messages.ExecuteLogicalMessagesBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.CallbackInvocationBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Messages.ExtractLogicalMessagesBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Sagas.RemoveIncomingHeadersBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.MessageMutator.ApplyIncomingTransportMessageMutatorsBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)", context.InnerExceptionOneStackTrace);

            StackTraceAssert.StartsWith(
string.Format(@"at NServiceBus.AcceptanceTests.Exceptions.When_handler_and_Uow_End_throws.Endpoint.UnitOfWorkThatThrows1.End(Exception ex)
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.AppendEndExceptionsAndRethrow(Exception initialException)"), context.InnerExceptionTwoStackTrace);
            
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
            public bool FirstOneExecuted { get; set; }
            public string TypeName { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Configurer.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance);
                    c.Configurer.ConfigureComponent<UnitOfWorkThatThrows1>(DependencyLifecycle.InstancePerUnitOfWork);
                    c.Configurer.ConfigureComponent<UnitOfWorkThatThrows2>(DependencyLifecycle.InstancePerUnitOfWork);
                    c.DisableTimeoutManager();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    })
                    .AllowExceptions();
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

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Begin()
                {
                    if (Context.FirstOneExecuted)
                    {
                        executedInSecondPlace = true;
                    }

                    Context.FirstOneExecuted = true;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
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

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Begin()
                {
                    if (Context.FirstOneExecuted)
                    {
                        executedInSecondPlace = true;
                    }

                    Context.FirstOneExecuted = true;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
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