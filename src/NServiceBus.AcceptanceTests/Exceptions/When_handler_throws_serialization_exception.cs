namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_handler_throws_serialization_exception : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_retry_the_message_using_flr()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                    .AllowExceptions()
                    .Done(c => c.HandedOverToSlr)
                    .Run();

            Assert.AreEqual(Context.MaximumRetries, context.NumberOfTimesInvoked);
            Assert.IsFalse(context.SerializationFailedCalled);
        }

        public class Context : ScenarioContext
        {
            public const int MaximumRetries = 3;

            public int NumberOfTimesInvoked { get; set; }
            public bool HandedOverToSlr { get; set; }
            public Dictionary<string, string> HeadersOfTheFailedMessage { get; set; }
            public bool SerializationFailedCalled { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public Context Context { get; set; }

            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                    b.RegisterComponents(r => r.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance)))
                     .WithConfig<TransportConfig>(c => c.MaxRetries = Context.MaximumRetries);
            }

            class CustomFaultManager : IManageMessageFailures
            {
                public Context Context { get; set; }

                public void SerializationFailedForMessage(TransportMessage message, Exception e)
                {
                    Context.SerializationFailedCalled = true;
                }

                public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
                {
                    Context.HandedOverToSlr = true;
                    Context.HeadersOfTheFailedMessage = message.Headers;
                }

                public void Init(Address address)
                {

                }
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    Context.NumberOfTimesInvoked++;
                    throw new SerializationException();
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
        }
    }
}