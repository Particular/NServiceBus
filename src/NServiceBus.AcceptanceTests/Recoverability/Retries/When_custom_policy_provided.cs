namespace NServiceBus.AcceptanceTests.Recoverability.Retries
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

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                    b.When(bus => bus.SendLocal(new MessageToBeRetried()))
                        .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run();

            Assert.That(context.Configuration.Immediate.MaxNumberOfRetries, Is.EqualTo(2));
            Assert.That(context.Configuration.Delayed.MaxNumberOfRetries, Is.EqualTo(2));
            Assert.That(context.Configuration.Delayed.TimeIncrease, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }

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
                    var testContext = (Context)context.ScenarioContext;

                    config.EnableFeature<TimeoutManager>();
                    config.Recoverability()
                        .Immediate(immediate => immediate.NumberOfRetries(2))
                        .Delayed(delayed => delayed.NumberOfRetries(2).TimeIncrease(TimeSpan.FromMinutes(1)))
                        .CustomPolicy((cfg, errorContext) =>
                        {
                            testContext.Configuration = cfg;

                            return RecoverabilityAction.MoveToError();
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