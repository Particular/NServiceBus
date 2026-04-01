#nullable enable

namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_inline_executing_with_batched_dispatch : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Parent_SendLocal_returns_before_nested_handler_completes()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(builder => builder
                .When(async (session, scenarioContext) =>
                {
                    var task = session.SendLocal(new Endpoint.ParentMessage
                    {
                        CorrelationId = scenarioContext.TestRunId
                    });

                    scenarioContext.ParentTaskCompletedBeforeHandler = task.IsCompleted;

                    await task;

                    scenarioContext.MarkAsCompleted();
                }))
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.ParentHandlerInvoked, Is.True);
            Assert.That(context.ChildHandlerInvoked, Is.True);
            Assert.That(context.ParentTaskCompletedBeforeHandler, Is.False);
        });
    }

    public class Context : ScenarioContext
    {
        public bool ParentHandlerInvoked { get; set; }
        public bool ChildHandlerInvoked { get; set; }
        public bool ParentTaskCompletedBeforeHandler { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>((config, runContext) =>
        {
            config.UseTransport(new InMemoryTransport(new InMemoryTransportOptions { InlineExecution = new() }));
            config.LimitMessageProcessingConcurrencyTo(1);
        });

        public class ParentMessage : ICommand
        {
            public Guid CorrelationId { get; set; }
        }

        public class ChildMessage : ICommand
        {
            public Guid CorrelationId { get; set; }
        }

        public class ParentHandler(Context scenarioContext) : IHandleMessages<ParentMessage>
        {
            public async Task Handle(ParentMessage message, IMessageHandlerContext context)
            {
                if (message.CorrelationId == scenarioContext.TestRunId)
                {
                    scenarioContext.ParentHandlerInvoked = true;
                    await context.SendLocal(new ChildMessage { CorrelationId = scenarioContext.TestRunId });
                }
            }
        }

        public class ChildHandler(Context scenarioContext) : IHandleMessages<ChildMessage>
        {
            public Task Handle(ChildMessage message, IMessageHandlerContext context)
            {
                if (message.CorrelationId == scenarioContext.TestRunId)
                {
                    scenarioContext.ChildHandlerInvoked = true;
                }
                return Task.CompletedTask;
            }
        }
    }
}