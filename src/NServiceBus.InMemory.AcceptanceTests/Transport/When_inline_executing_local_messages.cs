#nullable enable

namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_inline_executing_local_messages : NServiceBusAcceptanceTest
{
    [Test]
    public async Task MoveToErrorQueueOnFailure_false_discards_the_message_but_still_faults_the_sendlocal_task()
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
            Assert.That(CurrentBroker.GetOrCreateQueue("error").Count, Is.EqualTo(0));
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
            config.UseTransport(new InMemoryTransport(null, new InlineExecutionOptions
            {
                MoveToErrorQueueOnFailure = false
            }));
            config.LimitMessageProcessingConcurrencyTo(1);
            var recoverability = config.Recoverability();
            recoverability.Immediate(settings => settings.NumberOfRetries(0));
            recoverability.Delayed(settings => settings.NumberOfRetries(0));
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
                    throw new SimulatedException();
                }

                return Task.CompletedTask;
            }
        }
    }
}
