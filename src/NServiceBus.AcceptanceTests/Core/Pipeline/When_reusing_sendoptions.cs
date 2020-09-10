namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
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
                .Done(c => c.ReceivedMessageIds.Count >= 3)
                .Run();

            Assert.AreEqual(3, context.ReceivedMessageIds.Count);
            Assert.AreEqual(3, context.ReceivedMessageIds.Distinct().Count(), "the message ids should be distinct");
        }

        class Context : ScenarioContext
        {
            public ConcurrentQueue<string> ReceivedMessageIds = new ConcurrentQueue<string>();
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class CommandHandler : IHandleMessages<SomeCommand>
            {
                public CommandHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeCommand message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.ReceivedMessageIds.Enqueue(context.MessageId);
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class SomeCommand : ICommand
        {
        }
    }
}
