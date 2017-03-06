namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_Audit_OverrideTimeToBeReceived_set_and_native_receive_tx : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_not_start_and_show_error()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.EndpointsStarted)
                    .Run();
            }, Throws.Exception.Message.Contains("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ."));
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.UseTransport<MsmqTransport>()
                        .Transactions(TransportTransactionMode.ReceiveOnly);
                    config.AuditProcessedMessagesTo("someAuditQueue", TimeSpan.FromHours(1));
                });
            }
        }
    }
}