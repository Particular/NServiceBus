#nullable enable

namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_inline_executing_remote_send_returns_immediately : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Send_to_remote_address_returns_completed_task()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(builder => builder
                .When(async (session, scenarioContext) =>
                {
                    var task = session.Send(new Endpoint.RemoteMessage
                    {
                        CorrelationId = scenarioContext.TestRunId
                    });

                    scenarioContext.RemoteSendTaskWasCompletedImmediately = task.IsCompleted;

                    await task;
                    
                    scenarioContext.MarkAsCompleted();
                }))
            .Run();

        Assert.That(context.RemoteSendTaskWasCompletedImmediately, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool RemoteSendTaskWasCompletedImmediately { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>((config, runContext) =>
        {
            config.UseTransport(new InMemoryTransport(null, new InlineExecutionOptions()));
            config.LimitMessageProcessingConcurrencyTo(1);
            config.ConfigureRouting().RouteToEndpoint(typeof(RemoteMessage), "remote-endpoint");
        });

        public class RemoteMessage : ICommand
        {
            public Guid CorrelationId { get; set; }
        }
    }
}