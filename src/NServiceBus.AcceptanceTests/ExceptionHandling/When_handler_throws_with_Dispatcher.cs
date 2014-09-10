namespace NServiceBus.AcceptanceTests.ManageFailures
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    public class When_handler_throws_with_Dispatcher : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exception_from_handler()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                .Done(c => c.ExceptionReceived)
                .Run();
            Assert.AreEqual(typeof(HandlerException), context.ExceptionType);
#if (!DEBUG)
            StackTraceAssert.AreEqual(
@"at NServiceBus.AcceptanceTests.ManageFailures.When_handler_throws_with_Dispatcher.Endpoint.Handler.Handle(Message message)
at NServiceBus.Unicast.Behaviors.InvokeHandlersBehavior.<>c__DisplayClass2.<DispatchMessageToHandlersBasedOnType>b__0(Action dispatch)
at System.Collections.Generic.List`1.ForEach(Action`1 action)
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
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ForwardBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Audit.AuditBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ImpersonateSenderBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.MessageHandlingLoggingBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ChildContainerBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)
at NServiceBus.Transports.Msmq.MsmqDequeueStrategy.Action()", context.StackTrace);
#endif

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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Configurer.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance);
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
                    Context.ExceptionType = e.GetType();
                    Context.StackTrace = e.StackTrace;
                    Context.ExceptionReceived = true;
                }

                public void Init(Address address)
                {
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

            class MyDispatcher : IMessageDispatcherFactory
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public IEnumerable<Action> GetDispatcher(Type messageHandlerType, IBuilder builder, object toHandle)
                {
                    yield return () => new Handler().Handle((Message) toHandle);
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public bool CanDispatch(Type handler)
                {
                    return handler == typeof(Handler);
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
    }
}