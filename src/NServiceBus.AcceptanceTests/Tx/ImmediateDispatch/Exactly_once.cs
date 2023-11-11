﻿namespace NServiceBus.AcceptanceTests.Tx;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class Exactly_once : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_dispatch_immediately()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ExactlyOnceEndpoint>(b => b
                .When(session => session.SendLocal(new InitiatingMessage()))
                .DoNotFailOnErrorMessages())
            .Done(c => c.MessageDispatched)
            .Run();

        Assert.True(context.MessageDispatched, "Should dispatch the message immediately");
    }

    public class Context : ScenarioContext
    {
        public bool MessageDispatched { get; set; }
    }

    public class ExactlyOnceEndpoint : EndpointConfigurationBuilder
    {
        public ExactlyOnceEndpoint()
        {
            //note: We don't have a explicit way to request "ExactlyOnce" yet so we have to rely on it being the default
            EndpointSetup<DefaultServer>();
        }

        public class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
        {
            public async Task Handle(InitiatingMessage message, IMessageHandlerContext context)
            {
                var options = new SendOptions();

                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();

                await context.Send(new MessageToBeDispatchedImmediately(), options);

                throw new SimulatedException();
            }
        }

        public class MessageToBeDispatchedImmediatelyHandler : IHandleMessages<MessageToBeDispatchedImmediately>
        {
            public MessageToBeDispatchedImmediatelyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MessageToBeDispatchedImmediately message, IMessageHandlerContext context)
            {
                testContext.MessageDispatched = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class InitiatingMessage : ICommand
    {
    }

    public class MessageToBeDispatchedImmediately : ICommand
    {
    }
}