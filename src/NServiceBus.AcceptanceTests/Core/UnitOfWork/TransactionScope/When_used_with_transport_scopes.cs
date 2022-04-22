namespace NServiceBus.AcceptanceTests.Core.UnitOfWork.TransactionScope
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;

    public class When_used_with_transport_scopes : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_blow_up()
        {
            var exception = Assert.ThrowsAsync<Exception>(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<ScopeEndpoint>(b => b.CustomConfig(c =>
                    {
                        var fakeTransport = new FakeTransport
                        {
                            TransportTransactionMode = TransportTransactionMode.TransactionScope
                        };
                        c.UseTransport(fakeTransport);
                        c.UnitOfWork()
                            .WrapHandlersInATransactionScope();
                    }))
                    .Run();
            });

            Assert.True(exception.Message.Contains("A Transaction scope unit of work can't be used when the transport already uses a scope"));
        }

        public class ScopeEndpoint : EndpointFromTemplate<DefaultServer>
        {
        }
    }
}