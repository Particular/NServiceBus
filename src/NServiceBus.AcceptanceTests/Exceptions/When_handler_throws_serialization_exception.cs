namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_handler_throws_serialization_exception : NServiceBusAcceptanceTest
    {
        public static Func<int> MaxNumberOfRetries = () => 5;

        [Test]
        public void Should_retry_the_message_using_flr()
        {
            var context = new Context { Id = Guid.NewGuid() };

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, ctx) => bus.SendLocal(new MessageToBeRetried { ContextId = ctx.Id })))
                    .AllowExceptions()
                    .Done(c => c.HandedOverToSlr)
                    .Run(TimeSpan.FromMinutes(5));

            Assert.AreEqual(MaxNumberOfRetries(), context.NumberOfTimesInvoked);
            Assert.IsFalse(context.SerializationFailedCalled);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int NumberOfTimesInvoked { get; set; }
            public bool HandedOverToSlr { get; set; }
            public Dictionary<string, string> HeadersOfTheFailedMessage { get; set; }
            public bool SerializationFailedCalled { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b => b.RegisterComponents(r => r.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance)));
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
                    if (message.ContextId != Context.Id)
                    {
                        return;
                    }
                    Context.NumberOfTimesInvoked++;
                    throw new SerializationException();
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid ContextId { get; set; }
        }
    }
}