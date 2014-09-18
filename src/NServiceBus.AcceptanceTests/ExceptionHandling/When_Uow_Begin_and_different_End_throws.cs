﻿namespace NServiceBus.AcceptanceTests.ManageFailures
{
    using System;
    using System.Runtime.CompilerServices;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
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
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(BeginException), context.InnerExceptionOneType);
            Assert.AreEqual(typeof(EndException), context.InnerExceptionTwoType);

#if (!DEBUG)
            StackTraceAssert.AreEqual(
@"at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ForwardBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Audit.AuditBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ImpersonateSenderBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.MessageHandlingLoggingBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Behaviors.ChildContainerBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)
at NServiceBus.Transports.Msmq.MsmqDequeueStrategy.Action()", context.StackTrace);

            StackTraceAssert.AreEqual(
@"at NServiceBus.AcceptanceTests.ManageFailures.When_Uow_Begin_and_different_End_throws.Endpoint.UnitOfWorkThatThrowsInBegin.Begin()
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.Invoke(ReceivePhysicalMessageContext context, Action next)", context.InnerExceptionOneStackTrace);

            StackTraceAssert.AreEqual(
@"at NServiceBus.AcceptanceTests.ManageFailures.When_Uow_Begin_and_different_End_throws.Endpoint.UnitOfWorkThatThrowsInEnd.End(Exception ex)
at NServiceBus.UnitOfWork.UnitOfWorkBehavior.AppendEndExceptionsAndRethrow(Exception initialException)", context.InnerExceptionTwoStackTrace);

#endif

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
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Configurer.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance);
                    c.Configurer.ConfigureComponent<UnitOfWorkThatThrowsInBegin>(DependencyLifecycle.InstancePerUnitOfWork);
                    c.Configurer.ConfigureComponent<UnitOfWorkThatThrowsInEnd>(DependencyLifecycle.InstancePerUnitOfWork);
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

            public class UnitOfWorkThatThrowsInBegin : IManageUnitsOfWork
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void Begin()
                {
                    throw new BeginException();
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