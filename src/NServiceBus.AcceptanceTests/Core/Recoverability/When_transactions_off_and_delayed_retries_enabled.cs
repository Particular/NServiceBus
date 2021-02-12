﻿namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_transactions_off_and_delayed_retries_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_on_startup()
        {
            Requires.DelayedDelivery();

            var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<ScenarioContext>()
                .WithEndpoint<StartedEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("Delayed retries are not supported when running with TransportTransactionMode.None. Disable delayed retries using 'endpointConfiguration.Recoverability().Delayed(settings => settings.NumberOfRetries(0))' or select a different TransportTransactionMode.", exception.ToString());
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.None;
                    var recoverability = config.Recoverability();
                    recoverability.Delayed(i => i.NumberOfRetries(1));
                });
            }
        }
    }
}