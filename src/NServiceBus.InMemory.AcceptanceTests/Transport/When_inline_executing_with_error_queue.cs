#nullable enable

namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_inline_executing_with_error_queue : NServiceBusAcceptanceTest
{
    [Test]
    public async Task MoveToErrorQueueOnFailure_true_moves_to_error_and_faults_task()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(builder => builder
                .When(async (session, scenarioContext) =>
                {
                    try
                    {
                        await session.SendLocal(new Endpoint.StartMessage
                        {
                            CorrelationId = scenarioContext.TestRunId
                        });
                    }
                    catch (Exception ex)
                    {
                        scenarioContext.CaughtException = ex;
                    }
                    finally
                    {
                        scenarioContext.MarkAsCompleted();
                    }
                })
                .DoNotFailOnErrorMessages())
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.CaughtException, Is.InstanceOf<SimulatedException>());
            Assert.That(CurrentBroker.GetOrCreateQueue("error").Count, Is.EqualTo(1));
        });
    }

    public class Context : ScenarioContext
    {
        public Exception? CaughtException { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>((config, runContext) =>
        {
            config.UseTransport(new InMemoryTransport(new InMemoryTransportOptions
            {
                InlineExecution = new()
                {
                    MoveToErrorQueueOnFailure = true
                }
            }));
            config.SendFailedMessagesTo("error");
        });

        public class StartMessage : ICommand
        {
            public Guid CorrelationId { get; set; }
        }

        public class StartMessageHandler : IHandleMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new SimulatedException();
            }
        }
    }
}