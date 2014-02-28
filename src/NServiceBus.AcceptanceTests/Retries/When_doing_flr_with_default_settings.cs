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
        public static Func<int> X = () => 5;
            
        [Test]
        public void Should_do_X_retries_by_default_with_dtc_on()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, context) => bus.SendLocal(new MessageToBeRetried{ Id = context.Id })))
                    .Done(c => c.HandedOverToSlr || c.NumberOfTimesInvoked > X())
                    .Repeat(r => r.For<AllDtcTransports>())
                    .Should(c => Assert.AreEqual(X(), c.NumberOfTimesInvoked, string.Format("The FLR should by default retry {0} times", X())))
                    .Run();

        }

        [Test]
        public void Should_do_X_retries_by_default_with_native_transactions()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<RetryEndpoint>(b =>
                        {
                            b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DisableDistributedTransactions()));
                            b.Given((bus, context) => bus.SendLocal(new MessageToBeRetried { Id = context.Id }));
                        })
                    .Done(c => c.HandedOverToSlr || c.NumberOfTimesInvoked > X())
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.AreEqual(X(), c.NumberOfTimesInvoked, string.Format("The FLR should by default retry {0} times", X())))
                    .Run(TimeSpan.FromMinutes(X()));

        }

        [Test]
        public void Should_not_do_any_retries_if_transactions_are_off()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<RetryEndpoint>(b =>
                    {
                        b.CustomConfig(c => Configure.Transactions.Disable());
                        b.Given((bus, context) =>
                            {
                                bus.SendLocal(new MessageToBeRetried { Id = context.Id });
                                bus.SendLocal(new MessageToBeRetried { Id = context.Id, SecondMessage = true });
                            });
                    })
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
                    c => c.Configurer.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance))
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 1)
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