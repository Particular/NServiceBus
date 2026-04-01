#nullable enable

namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_inline_executing_local_messages_happy_path : NServiceBusAcceptanceTest
{
    [Test]
    public async Task SendLocal_task_completes_when_handler_finishes_successfully()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(builder => builder
                .When(async (session, scenarioContext) =>
                {
                    await session.SendLocal(new Endpoint.StartMessage
                    {
                        CorrelationId = scenarioContext.TestRunId
                    });
                    
                    scenarioContext.MarkAsCompleted();
                }))
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.HandlerInvoked, Is.True);
        });
    }

    public class Context : ScenarioContext
    {
        public bool HandlerInvoked { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>((config, runContext) =>
        {
            config.UseTransport(new InMemoryTransport(new InMemoryTransportOptions { InlineExecution = new() }));
            config.LimitMessageProcessingConcurrencyTo(1);
        });

        public class StartMessage : ICommand
        {
            public Guid CorrelationId { get; set; }
        }

        public class StartMessageHandler(Context scenarioContext) : IHandleMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                if (message.CorrelationId == scenarioContext.TestRunId)
                {
                    scenarioContext.HandlerInvoked = true;
                }

                return Task.CompletedTask;
            }
        }
    }
}