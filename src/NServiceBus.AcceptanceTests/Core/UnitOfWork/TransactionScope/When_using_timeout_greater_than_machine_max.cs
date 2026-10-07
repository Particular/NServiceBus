#nullable enable

namespace NServiceBus.AcceptanceTests.Core.UnitOfWork.TransactionScope;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_using_timeout_greater_than_machine_max : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_blow_up()
    {
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await Scenario.Define<ScenarioContext>()
                .WithEndpoint<ScopeEndpoint>()
                .Run();
        });

        Assert.That(exception.Message, Does.Contain("Timeout requested is longer than the maximum value for this machine"));
    }

    public class ScopeEndpoint : EndpointConfigurationBuilder
    {
        public ScopeEndpoint()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.UnitOfWork()
                    .WrapHandlersInATransactionScope(TimeSpan.FromHours(1));
            });
        }
    }
}