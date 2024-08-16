namespace NServiceBus.AcceptanceTests.Recoverability;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_custom_policy_discards_failed_message : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_consume_message_without_retries()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithDiscardPolicy>(e => e
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.HandlerInvoked >= 1)
            .Run();

        Assert.IsEmpty(context.FailedMessages, "the message should not be moved to the error queue");
        Assert.That(context.HandlerInvoked, Is.EqualTo(1), "the discarded message should not be retried");
    }

    class Context : ScenarioContext
    {
        public int HandlerInvoked { get; set; }
    }

    class EndpointWithDiscardPolicy : EndpointConfigurationBuilder
    {
        public EndpointWithDiscardPolicy() =>
            EndpointSetup<DefaultServer>(c => c
                .Recoverability()
                .CustomPolicy((config, context) => RecoverabilityAction.Discard("discard on purpose")));
    }

    class FailingMessageHandler : IHandleMessages<FailingMessage>
    {
        Context testContext;

        public FailingMessageHandler(Context testContext)
        {
            this.testContext = testContext;
        }

        public Task Handle(FailingMessage message, IMessageHandlerContext context)
        {
            testContext.HandlerInvoked++;
            throw new SimulatedException();
        }
    }

    public class FailingMessage : IMessage
    {
    }
}