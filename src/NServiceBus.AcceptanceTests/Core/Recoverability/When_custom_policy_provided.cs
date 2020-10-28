namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_custom_policy_provided : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_pass_recoverability_configuration()
        {
            Requires.DelayedDelivery();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                    b.When(bus => bus.SendLocal(new MessageToBeRetried()))
                        .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.That(context.Configuration.Immediate.MaxNumberOfRetries, Is.EqualTo(MaxImmediateRetries));
            Assert.That(context.Configuration.Delayed.MaxNumberOfRetries, Is.EqualTo(MaxDelayedRetries));
            Assert.That(context.Configuration.Delayed.TimeIncrease, Is.EqualTo(DelayedRetryDelayIncrease));
        }

        static TimeSpan DelayedRetryDelayIncrease = TimeSpan.FromMinutes(1);
        const int MaxImmediateRetries = 2;
        const int MaxDelayedRetries = 2;

        class Context : ScenarioContext
        {
            public RecoverabilityConfig Configuration { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = (Context) context.ScenarioContext;

                    config.Recoverability()
                        .Immediate(immediate => immediate.NumberOfRetries(MaxImmediateRetries))
                        .Delayed(delayed => delayed.NumberOfRetries(MaxDelayedRetries).TimeIncrease(DelayedRetryDelayIncrease))
                        .CustomPolicy((cfg, errorContext) =>
                        {
                            testContext.Configuration = cfg;

                            return RecoverabilityAction.MoveToError(cfg.Failed.ErrorQueue);
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