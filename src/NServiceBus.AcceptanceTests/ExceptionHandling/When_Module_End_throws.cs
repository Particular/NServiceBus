#pragma warning disable 0618
namespace NServiceBus.AcceptanceTests.ManageFailures
{
    using System;
    using System.Runtime.CompilerServices;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_Module_End_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exception_from_end()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(EndException), context.ExceptionType);
#if (!DEBUG)
            StackTraceAssert.AreEqual(
@"at NServiceBus.AcceptanceTests.ManageFailures.When_Module_End_throws.Endpoint.MessageModuleThatThrowsInEnd.HandleEndMessage()
at System.Collections.Generic.List`1.ForEach(Action`1 action)
at NServiceBus.Unicast.UnicastBus.TransportFinishedMessageProcessing(Object sender, FinishedMessageProcessingEventArgs e)
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

            class MessageModuleThatThrowsInEnd:IMessageModule
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleBeginMessage()
                {
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleEndMessage()
                {
                    throw new EndException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleError()
                {
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