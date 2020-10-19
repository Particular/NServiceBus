namespace NServiceBus.AcceptanceTests.Core.Recoverability
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
            Assert.That(context.ErrorContexts[0].DelayedDeliveriesPerformed, Is.EqualTo(0));
            Assert.That(context.ErrorContexts[1].Message, Is.Not.Null);
            Assert.That(context.ErrorContexts[1].Exception, Is.TypeOf<SimulatedException>());
            Assert.That(context.ErrorContexts[1].DelayedDeliveriesPerformed, Is.EqualTo(1));
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

                    config.Recoverability()
                        .CustomPolicy((cfg, errorContext) =>
                        {
                            testContext.ErrorContexts.Add(errorContext);

                            if (errorContext.DelayedDeliveriesPerformed >= 1)
                            {
                                return RecoverabilityAction.MoveToError(cfg.Failed.ErrorQueue);
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

        public class MessageToBeRetried : IMessage
        {
        }
    }
}