namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_reusing_sendoptions : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_generate_new_message_id_for_every_message()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e
                .When(async s =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    await s.Send(new SomeCommand(), sendOptions);
                    await s.Send(new SomeCommand(), sendOptions);
                    await s.Send(new SomeCommand(), sendOptions);
                }))
            .Run();

        Assert.That(context.ReceivedMessageIds, Has.Count.EqualTo(3));
        Assert.That(context.ReceivedMessageIds.Distinct().Count(), Is.EqualTo(3), "the message ids should be distinct");
    }

    class Context : ScenarioContext
    {
        public ConcurrentQueue<string> ReceivedMessageIds { get; } = new();
    }

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        class CommandHandler(Context testContext) : IHandleMessages<SomeCommand>
        {
            public Task Handle(SomeCommand message, IMessageHandlerContext context)
            {
                testContext.ReceivedMessageIds.Enqueue(context.MessageId);
                testContext.MarkAsCompleted(testContext.ReceivedMessageIds.Count >= 3);
                return Task.CompletedTask;
            }
        }
    }

    public class SomeCommand : ICommand;
}