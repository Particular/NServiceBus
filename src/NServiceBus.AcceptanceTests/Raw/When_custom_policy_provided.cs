namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_custom_policy_provided_for_raw : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_pass_recoverability_configuration()
        {
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
            public bool RawCalled { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = (Context) context.ScenarioContext;

                    config.Raw((message, dispatcher) =>
                    {
                        testContext.RawCalled = true;
                        throw new SimulatedException();
                    });

                    config.EnableFeature<TimeoutManager>();
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
        }

        public class MessageToBeRetried : IMessage
        {
        }
    }
}