namespace NServiceBus.AcceptanceTests.Core.Routing;

using System;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_non_message_with_routing_configured_by_assembly : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_throw_when_sending() =>
        Assert.That(async () => await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(async (session, c) =>
            {
                try
                {
                    await session.Send(new NonMessage());
                }
                catch (Exception ex)
                {
                    c.MarkAsFailed(ex);
                }
            }))
            .Run(), Throws.Exception.Message.Contains("No destination specified for message"));

    public class Context : ScenarioContext;

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(NonMessage).Assembly, "Destination");
            });
    }

    public class NonMessage;
}