﻿namespace NServiceBus.AcceptanceTests.Exceptions
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

    public class When_Uow_End_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exception_thrown_from_end()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(EndException), context.ExceptionType);
            StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.When_Uow_End_throws.Endpoint.UnitOfWorkThatThrowsInEnd.End(Exception ex)
at NServiceBus.UnitOfWorkBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ChildContainerBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.ProcessingStatisticsBehavior.Invoke(IncomingContext context, Action next)
at NServiceBus.Pipeline.PipelineExecutor.Execute[T](BehaviorChain`1 pipelineAction, T context)
at NServiceBus.Unicast.Transport.TransportReceiver.ProcessMessage(TransportMessage message)
at NServiceBus.Unicast.Transport.TransportReceiver.TryProcess(TransportMessage message)", context.StackTrace);
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
                    b.RegisterComponents(c =>
                    {
                        c.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance);
                        c.ConfigureComponent<UnitOfWorkThatThrowsInEnd>(DependencyLifecycle.InstancePerUnitOfWork);
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
                    Context.ExceptionType = e.GetType();
                    Context.StackTrace = e.StackTrace;
                    Context.ExceptionReceived = true;
                }

                public void Init(Address address)
                {

                }
            }

            class UnitOfWorkThatThrowsInEnd : IManageUnitsOfWork
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
        public class EndException : Exception
        {
            public EndException()
                : base("EndException")
            {

            }
        }
    }

}