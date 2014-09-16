namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using System.Collections.Generic;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_fails_with_retries_set_to_0 : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_retry_the_message_using_flr()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                    .AllowExceptions()
                    .Done(c => c.HandedOverToSlr)
                    .Run();

            Assert.AreEqual(1, context.NumberOfTimesInvoked,"No FLR should be in use if MaxRetries is set to 0");
        }

        public class Context : ScenarioContext
        {
            public int NumberOfTimesInvoked { get; set; }

            public bool HandedOverToSlr { get; set; }

            public Dictionary<string, string> HeadersOfTheFailedMessage { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b => b.RegisterComponents(r => r.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance)))
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class CustomFaultManager: IManageMessageFailures
            {
                public Context  Context { get; set; }

                public void SerializationFailedForMessage(TransportMessage message, Exception e)
                {
                    
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

            class MessageToBeRetriedHandler:IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }
                public void Handle(MessageToBeRetried message)
                {
                    Context.NumberOfTimesInvoked++;
                    throw new Exception("Simulated exception");
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
        }
    }


}