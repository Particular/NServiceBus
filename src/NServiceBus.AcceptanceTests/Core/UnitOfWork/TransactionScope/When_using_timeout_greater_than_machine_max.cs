﻿namespace NServiceBus.AcceptanceTests.Core.UnitOfWork.TransactionScope
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_timeout_greater_than_machine_max : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_blow_up()
        {
            var aex = Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<ScopeEndpoint>()
                    .Run();
            });

            Assert.True(aex.InnerException.InnerException.Message.Contains("Timeout requested is longer than the maximum value for this machine"));
        }

        public class ScopeEndpoint : EndpointConfigurationBuilder
        {
            public ScopeEndpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseTransport(r.GetTransportType())
                        .Transactions(TransportTransactionMode.ReceiveOnly);
                    c.UnitOfWork()
                        .WrapHandlersInATransactionScope(TimeSpan.FromHours(1));
                });
            }
        }
    }
}