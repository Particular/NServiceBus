﻿namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System;
    using System.Configuration;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_scope_timeout_greater_than_machine_max : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_blow_up()
        {
            var exception = Assert.ThrowsAsync<ConfigurationErrorsException>(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                        .WithEndpoint<ScopeEndpoint>()
                        .Run();
            });

            Assert.True(exception.Message.Contains("Timeout requested is longer than the maximum value for this machine"));
        }

        public class ScopeEndpoint : EndpointConfigurationBuilder
        {
            public ScopeEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>()
                        .Transactions(TransportTransactionMode.TransactionScope)
                        .TransactionScopeOptions(timeout: TimeSpan.FromHours(1));
                });
            }
        }
    }
}