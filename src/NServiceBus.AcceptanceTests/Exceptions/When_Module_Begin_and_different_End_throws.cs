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

    public class When_Module_Begin_and_different_End_throws : NServiceBusAcceptanceTest
    {
        [Test]
        [Explicit]
        public void Should_receive_AggregateException_with_both_exceptions()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new Message())))
                    .Done(c => c.ExceptionReceived)
                    .Run();

            var exceptionType = context.ExceptionType;
            Assert.AreEqual(typeof(AggregateException), exceptionType);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string StackTrace { get; set; }
            public Type ExceptionType { get; set; }
            public string Log { get; set; }
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

            class MessageModuleThatThrowsInBegin : IMessageModule
            {
                public Context Context { get; set; }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleBeginMessage()
                {
                    Context.Log += "ThrowsInBegin Begin\r\n";
                    throw new BeginException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleEndMessage()
                {
                    Context.Log += "ThrowsInBegin Begin\r\n";
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleError()
                {
                    Context.Log += "ThrowsInBegin Begin\r\n";
                }
            }

            class MessageModuleThatThrowsInEnd : IMessageModule
            {
                public Context Context { get; set; }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleBeginMessage()
                {
                    Context.Log += "ThrowsInEnd Begin\r\n";
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleEndMessage()
                {
                    Context.Log += "ThrowsInEnd End\r\n";
                    throw new EndException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                public void HandleError()
                {
                    Context.Log += "ThrowsInEnd Error\r\n";
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