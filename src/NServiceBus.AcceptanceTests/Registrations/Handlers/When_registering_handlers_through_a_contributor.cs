namespace NServiceBus.AcceptanceTests.Registrations.Handlers;

using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_registering_handlers_through_a_contributor : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_message_with_handler_added_by_the_contributor()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointUsingContributor>(b => b.When(async (session, _) => await session.SendLocal(new ContributedMessage())))
            .Run();

        Assert.That(context.ContributedHandlerReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool ContributedHandlerReceived;
    }

    public class EndpointUsingContributor : EndpointConfigurationBuilder
    {
        public EndpointUsingContributor() => EndpointSetup<NonScanningServer>(config => config.Handlers.All.ContributedRegistrations.AddAll());
    }

    public class ContributedMessage : IMessage;

    [Handler]
    public class ContributedHandler(Context testContext) : IHandleMessages<ContributedMessage>
    {
        public Task Handle(ContributedMessage message, IMessageHandlerContext context)
        {
            testContext.ContributedHandlerReceived = true;
            testContext.MarkAsCompleted();
            return Task.CompletedTask;
        }
    }
}
