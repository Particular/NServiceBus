namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_doing_flr_with_default_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_do_any_retries_if_transactions_are_off()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, context) =>
                    {
                        bus.SendLocal(new MessageToBeRetried { Id = context.Id });
                        bus.SendLocal(new MessageToBeRetried { Id = context.Id, SecondMessage = true });
                    }))
                    .AllowExceptions()
                    .Done(c => c.SecondMessageReceived || c.NumberOfTimesInvoked > 1)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.AreEqual(1, c.NumberOfTimesInvoked, "No retries should be in use if transactions are off"))
                    .Run();

        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public bool HandedOverToSlr { get; set; }

            public bool SecondMessageReceived { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.Transactions().Disable();
                        b.RegisterComponents(r => r.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance));
                    })
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 1);
            }

            class CustomFaultManager : IManageMessageFailures
            {
                public Context Context { get; set; }

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

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public void Handle(MessageToBeRetried message)
                {
                    if (message.Id != Context.Id) return; // messages from previous test runs must be ignored

                    if (message.SecondMessage)
                    {
                        Context.SecondMessageReceived = true;
                        return;
                    }

                    Context.NumberOfTimesInvoked++;

                    throw new Exception("Simulated exception");
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }

            public bool SecondMessage { get; set; }
        }
    }


}