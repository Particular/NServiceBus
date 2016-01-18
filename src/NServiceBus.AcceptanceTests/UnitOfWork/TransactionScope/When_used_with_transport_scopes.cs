namespace NServiceBus.AcceptanceTests.UnitOfWork
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_used_with_transport_scopes
    {
        [Test]
        public void Should_blow_up()
        {
            var aex = Assert.Throws<AggregateException>(async () =>
            {
                await Scenario.Define<Context>()
                        .WithEndpoint<ScopeEndpoint>()
                        .Repeat(b => b.For<AllDtcTransports>())
                        .Run();
            });

            Assert.True(aex.InnerException.InnerException.Message.Contains("A Transaction scope unit of work can't be used when the transport already uses a scope"));
        }

        public class Context : ScenarioContext
        {
        }

        public class ScopeEndpoint : EndpointConfigurationBuilder
        {
            public ScopeEndpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseTransport(r.GetTransportType())
                        .Transactions(TransportTransactionMode.TransactionScope);
                    c.UnitOfWork()
                        .WrapHandlersInATransactionScope();
                });
            }
        }
    }
}