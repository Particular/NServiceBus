namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_doing_flr_with_default_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_do_5_retries_by_default()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                    .Done(c => c.HandedOverToSlr)
                    .Run();

            Assert.AreEqual(5, context.NumberOfTimesInvoked,"The FLR should by default retry 5 times");
        }

        public class Context : ScenarioContext
        {
            public int NumberOfTimesInvoked { get; set; }

            public bool HandedOverToSlr { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Configurer.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance));
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