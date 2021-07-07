namespace NServiceBus.AcceptanceTests.Core.UnitOfWork.TransactionScope
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;

    public class When_used_with_default_transaction_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work()
        {
            var context = await Scenario.Define<ScenarioContext>()
                 .WithEndpoint<ScopeEndpoint>(b => b.CustomConfig(c =>
                 {
                     var transport = new FakeTransport
                     {
                         TransportTransactionMode = TransportTransactionMode.ReceiveOnly
                     };
                     c.UseTransport(transport);
                     c.UnitOfWork()
                         .WrapHandlersInATransactionScope();
                 }))
                 .Done(c => c.EndpointsStarted)
                 .Run();

            Assert.True(context.EndpointsStarted);
        }

        public class ScopeEndpoint : EndpointConfigurationBuilder
        {
            public ScopeEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }
    }
}