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
    using Transport;

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

            Assert.That(context.SlrRetryContexts.Count, Is.EqualTo(2), "because the custom policy should have been invoked twice");
            Assert.That(context.SlrRetryContexts[0].Message, Is.Not.Null);
            Assert.That(context.SlrRetryContexts[0].Exception, Is.TypeOf<SimulatedException>());
            Assert.That(context.SlrRetryContexts[0].NumberOfDelayedDeliveryAttempts, Is.EqualTo(1));
            Assert.That(context.SlrRetryContexts[1].Message, Is.Not.Null);
            Assert.That(context.SlrRetryContexts[1].Exception, Is.TypeOf<SimulatedException>());
            Assert.That(context.SlrRetryContexts[1].NumberOfDelayedDeliveryAttempts, Is.EqualTo(2));
        }

        class Context : ScenarioContext
        {
            public List<ErrorContext> SlrRetryContexts { get; } = new List<ErrorContext>();
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    int slrRetries = 0;
                    var testContext = (Context) context.ScenarioContext;

                    config.EnableFeature<TimeoutManager>();
                    config.Recoverability()
                        .PolicyOverride((cfg, errorContext) =>
                        {
                            testContext.SlrRetryContexts.Add(errorContext);

                            if (slrRetries++ >= 1)
                            {
                                return RecoverabilityAction.MoveToError();
                            }

                            return RecoverabilityAction.DelayedRetry(TimeSpan.FromMilliseconds(1));
                        });
                });
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