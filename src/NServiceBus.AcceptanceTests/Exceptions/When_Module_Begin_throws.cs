#pragma warning disable 0618
namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Runtime.CompilerServices;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_Module_Begin_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_exception_from_begin()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(BeginException), context.ExceptionType);
#if (!DEBUG)
            StackTraceAssert.StartsWith(
@"at NServiceBus.AcceptanceTests.Exceptions.When_Module_Begin_throws.Endpoint.MessageModuleThatThrowsInBegin.HandleBeginMessage()
at System.Collections.Generic.List`1.ForEach(Action`1 action)
at NServiceBus.Unicast.UnicastBus.TransportStartedMessageProcessing(Object sender, StartedMessageProcessingEventArgs e)
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

            class MessageModuleThatThrowsInBegin:IMessageModule
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleBeginMessage()
                {
                    throw new BeginException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleEndMessage()
                {
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
        public class BeginException : Exception
        {
            public BeginException()
                : base("BeginException")
            {

            }
        }
    }

}