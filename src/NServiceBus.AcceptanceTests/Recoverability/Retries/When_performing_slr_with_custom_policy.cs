namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Transports;
    using ErrorContext= System.Tuple<Transports.IncomingMessage, System.Exception, int>;

    public class When_performing_slr_with_custom_policy : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_expose_error_context_to_policy()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                    b.When(bus => bus.SendLocal(new MessageToBeRetried()))
                     .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.That(context.HeaderValues.Count, Is.EqualTo(2), "because the custom policy should have been invoked twice");
            Assert.That(context.HeaderValues[0].Item1, Is.Not.Null);
            Assert.That(context.HeaderValues[0].Item2, Is.TypeOf<SimulatedException>());
            Assert.That(context.HeaderValues[0].Item3, Is.EqualTo(1));
            Assert.That(context.HeaderValues[1].Item1, Is.Not.Null);
            Assert.That(context.HeaderValues[1].Item2, Is.TypeOf<SimulatedException>());
            Assert.That(context.HeaderValues[1].Item3, Is.EqualTo(2));
        }

        class Context : ScenarioContext
        {
            public bool MessageMovedToErrorQueue { get; set; }
            public List<ErrorContext> HeaderValues { get; set; } = new List<ErrorContext>();
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
                    config.SecondLevelRetries().CustomRetryPolicy(new CustomPolicy(testContext).GetDelay);
                });
            }

            class CustomPolicy
            {
                public CustomPolicy(Context context)
                {
                    this.context = context;
                }

                public TimeSpan GetDelay(IncomingMessage msg, Exception exception, int retryAttempt)
                {
                    context.HeaderValues.Add(new ErrorContext(msg, exception, retryAttempt));

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