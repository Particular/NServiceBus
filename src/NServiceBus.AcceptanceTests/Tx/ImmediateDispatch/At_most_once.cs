namespace NServiceBus.AcceptanceTests.Tx;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class At_most_once : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_dispatch_immediately()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<NonTransactionalEndpoint>(b => b
                .When(session => session.SendLocal(new InitiatingMessage()))
                .DoNotFailOnErrorMessages())
            .Run();

        Assert.That(context.MessageDispatched, Is.True, "Should dispatch the message immediately");
    }

    public class Context : ScenarioContext
    {
        public bool MessageDispatched { get; set; }
    }

    public class NonTransactionalEndpoint : EndpointConfigurationBuilder
    {
        public NonTransactionalEndpoint() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.None;
            });

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

        public class MessageToBeDispatchedImmediatelyHandler(Context testContext) : IHandleMessages<MessageToBeDispatchedImmediately>
        {
            public Task Handle(MessageToBeDispatchedImmediately message, IMessageHandlerContext context)
            {
                testContext.MessageDispatched = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class InitiatingMessage : ICommand;

    public class MessageToBeDispatchedImmediately : ICommand;
}