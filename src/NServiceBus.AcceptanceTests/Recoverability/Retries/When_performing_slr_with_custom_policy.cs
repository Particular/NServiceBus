namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Transports;

    public class When_performing_slr_with_custom_policy : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_expose_headers_to_policy()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => 
                    b.When(bus => bus.SendLocal(new MessageToBeRetried()))
                     .DoNotFailOnErrorMessages())
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.That(context.FLRetriesHeaderAvailable, Is.True, "Could not find FLRetries header");
            Assert.That(context.RetriesHeaderAvailable, Is.True, "Could not find Retries header");
        }

        class Context : ScenarioContext
        {
            public bool FLRetriesHeaderAvailable { get; set; }
            public bool MessageMovedToErrorQueue { get; set; }
            public bool RetriesHeaderAvailable { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = context.ScenarioContext as Context;

                    config.EnableFeature<TimeoutManager>();
                    config.EnableFeature<FirstLevelRetries>();
                    config.EnableFeature<SecondLevelRetries>();
                    config.Notifications.Errors.MessageSentToErrorQueue += (sender, message) => { testContext.MessageMovedToErrorQueue = true; };
                    config.SecondLevelRetries().CustomRetryPolicy(new CustomPolicy(testContext).GetDelay);
                });
            }

            class CustomPolicy
            {
                public CustomPolicy(Context context)
                {
                    this.context = context;
                }

                public TimeSpan GetDelay(IncomingMessage msg)
                {
                    context.FLRetriesHeaderAvailable |= msg.Headers.ContainsKey(Headers.FLRetries);
                    context.RetriesHeaderAvailable |= msg.Headers.ContainsKey(Headers.Retries);

                    if (slrRetries++ == 1)
                    {
                        return TimeSpan.MinValue;
                    }
                    return TimeSpan.FromMilliseconds(1);
                }

                Context context;
                int slrRetries;
            }

            class Handler : IHandleMessages<MessageToBeRetried>
            {
                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        class MessageToBeRetried : IMessage
        {
        }
    }
}