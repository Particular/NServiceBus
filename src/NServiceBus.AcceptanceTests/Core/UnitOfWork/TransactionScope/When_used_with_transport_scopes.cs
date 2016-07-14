﻿namespace NServiceBus.AcceptanceTests.Core.UnitOfWork.TransactionScope
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
            var aex = Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<ScopeEndpoint>(b => b.CustomConfig(c =>
                    {
                        c.UseTransport<FakeTransport>()
                            .Transactions(TransportTransactionMode.TransactionScope);
                        c.UnitOfWork()
                            .WrapHandlersInATransactionScope();
                    }))
                    .Run();
            });

            Assert.True(aex.InnerException.InnerException.Message.Contains("A Transaction scope unit of work can't be used when the transport already uses a scope"));
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