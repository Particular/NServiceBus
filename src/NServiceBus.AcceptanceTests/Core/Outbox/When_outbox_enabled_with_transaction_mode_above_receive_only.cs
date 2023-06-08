namespace NServiceBus.AcceptanceTests.Outbox
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_outbox_enabled_with_transaction_mode_above_receive_only : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_fail_to_start()
        {
            var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(_ => false)
                .Run());

            StringAssert.Contains($"Outbox requires transport to be running in `{nameof(TransportTransactionMode.ReceiveOnly)}` mode", exception.Message);
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableOutbox();
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
                });
            }
        }
    }
}