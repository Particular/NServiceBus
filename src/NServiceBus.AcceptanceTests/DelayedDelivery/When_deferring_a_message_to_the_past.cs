﻿namespace NServiceBus.AcceptanceTests.DelayedDelivery;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_deferring_a_message_to_the_past : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_deliver_message()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When((bus, c) =>
            {
                var options = new SendOptions();

                options.DoNotDeliverBefore(DateTimeOffset.UtcNow.AddHours(-1));
                options.RouteToThisEndpoint();

                return bus.Send(new MyMessage(), options);
            }))
            .Done(c => c.MessageReceived)
            .Run();

        Assert.That(context.MessageReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class MyMessage : IMessage
    {
    }
}