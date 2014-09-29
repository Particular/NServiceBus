namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.CompilerServices;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Faults;
    using NUnit.Framework;

    public class When_handler_throws : NServiceBusAcceptanceTest
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
      StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.When_handler_throws.Endpoint.Handler.Handle(Message message)
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
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ForwardBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Audit.AuditBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ImpersonateSenderBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.MessageHandlingLoggingBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ChildContainerBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)", context.StackTrace);
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