namespace NServiceBus.AcceptanceTests.Core.UnitOfWork.TransactionScope
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;

    public class When_transport_supports_transaction_scope_on_windows : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_enable_dtc()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Ignore("Only relevant on windows");
            }

            var fakeTransport = new FakeTransport
            {
                TransportTransactionMode = TransportTransactionMode.TransactionScope
            };

            await Scenario.Define<ScenarioContext>()
                .WithEndpoint<TransactionScopeEndpoint>(b => b.CustomConfig(c =>
                {
                    c.UseTransport(fakeTransport);
                }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(fakeTransport.DtcIsAvailable, fakeTransport.DtcCheckException?.Message);
        }

        public class TransactionScopeEndpoint : EndpointConfigurationBuilder
        {
            public TransactionScopeEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }
    }
}