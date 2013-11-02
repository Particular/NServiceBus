﻿namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using Config;
    using Faults;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_doing_flr_with_default_settings : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_do_5_retries_by_default_with_dtc_on()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<RetryEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeRetried())))
                    .Done(c => c.HandedOverToSlr || c.NumberOfTimesInvoked > 5)
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.AreEqual(5, c.NumberOfTimesInvoked, "The FLR should by default retry 5 times"))
                    .Run();

        }

        [Test]
        public void Should_do_5_retries_by_default_with_native_transactions()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<RetryEndpoint>(b =>
                        {
                            b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DisableDistributedTransactions()));
                            b.Given(bus => bus.SendLocal(new MessageToBeRetried()));
                        })
                    .Done(c => c.HandedOverToSlr || c.NumberOfTimesInvoked > 5)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.AreEqual(5, c.NumberOfTimesInvoked, "The FLR should by default retry 5 times"))
                    .Run();

        }

        [Test]
        public void Should_not_do_any_retries_if_transactions_are_off()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<RetryEndpoint>(b =>
                    {
                        b.CustomConfig(c => Configure.Transactions.Disable());
                        b.Given((bus, context) =>
                            {
                                bus.SendLocal(new MessageToBeRetried());
                                bus.SendLocal(new MessageToBeRetried { SecondMessage = true });
                            });
                    })
                    .Done(c => c.SecondMessageReceived || c.NumberOfTimesInvoked > 1)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.AreEqual(1, c.NumberOfTimesInvoked, "No retries should be in use if transactions are off"))
                    .Run();

        }

        public class Context : ScenarioContext
        {
            public int NumberOfTimesInvoked { get; set; }

            public bool HandedOverToSlr { get; set; }

            public bool SecondMessageReceived { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    c => c.Configurer.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance))
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
            public bool SecondMessage { get; set; }
        }
    }


}