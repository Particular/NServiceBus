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

    public class When_custom_policy_executed : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_provide_error_context_to_policy()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                    b.When(bus => bus.SendLocal(new MessageToBeRetried()))
                     .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.That(context.ErrorContexts.Count, Is.EqualTo(2), "because the custom policy should have been invoked twice");
            Assert.That(context.ErrorContexts[0].Message, Is.Not.Null);
            Assert.That(context.ErrorContexts[0].Exception, Is.TypeOf<SimulatedException>());
            Assert.That(context.ErrorContexts[0].NumberOfDelayedDeliveryAttempts, Is.EqualTo(1));
            Assert.That(context.ErrorContexts[1].Message, Is.Not.Null);
            Assert.That(context.ErrorContexts[1].Exception, Is.TypeOf<SimulatedException>());
            Assert.That(context.ErrorContexts[1].NumberOfDelayedDeliveryAttempts, Is.EqualTo(2));
        }

        class Context : ScenarioContext
        {
            public List<ErrorContext> ErrorContexts { get; } = new List<ErrorContext>();
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = (Context) context.ScenarioContext;

                    config.EnableFeature<TimeoutManager>();
                    config.Recoverability()
                        .CustomPolicy((cfg, errorContext) =>
                        {
                            testContext.ErrorContexts.Add(errorContext);

                            if (errorContext.NumberOfDelayedDeliveryAttempts >= 2)
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