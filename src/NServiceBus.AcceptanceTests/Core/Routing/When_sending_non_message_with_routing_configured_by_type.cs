﻿namespace NServiceBus.AcceptanceTests.Core.Routing;

using System;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

//All other variants of configuration-time routing checks are covered by unit tests.
public class When_sending_non_message_with_routing_configured_by_type : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_throw_when_configuring_routing()
    {
        var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(c => c
                .When(b => b.Send(new NonMessage())))
            .Done(c => c.EndpointsStarted)
            .Run());

        StringAssert.Contains("Cannot configure routing for type", exception.ToString());
    }

    public class Context : ScenarioContext
    {
        public bool GotTheException { get; set; }
        public Exception Exception { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(NonMessage), "Destination");
            });
        }
    }

    public class NonMessage
    {
    }
}