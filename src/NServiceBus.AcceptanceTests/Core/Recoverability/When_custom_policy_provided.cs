namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using NUnit.Framework;

public class When_custom_policy_provided : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_pass_recoverability_configuration()
    {
        Requires.DelayedDelivery();

        var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b =>
                b.When(bus => bus.SendLocal(new MessageToBeRetried())))
            .Run());

        var context = (Context)exception.ScenarioContext;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Configuration.Immediate.MaxNumberOfRetries, Is.EqualTo(MaxImmediateRetries));
            Assert.That(context.Configuration.Delayed.MaxNumberOfRetries, Is.EqualTo(MaxDelayedRetries));
            Assert.That(context.Configuration.Delayed.TimeIncrease, Is.EqualTo(DelayedRetryDelayIncrease));
        }
    }

    static readonly TimeSpan DelayedRetryDelayIncrease = TimeSpan.FromMinutes(1);
    const int MaxImmediateRetries = 2;
    const int MaxDelayedRetries = 2;

    public class Context : ScenarioContext
    {
        public RecoverabilityConfig Configuration { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                var testContext = (Context)context.ScenarioContext;

                config.Recoverability()
                    .Immediate(immediate => immediate.NumberOfRetries(MaxImmediateRetries))
                    .Delayed(delayed => delayed.NumberOfRetries(MaxDelayedRetries).TimeIncrease(DelayedRetryDelayIncrease))
                    .CustomPolicy((cfg, errorContext) =>
                    {
                        testContext.Configuration = cfg;

                        return RecoverabilityAction.MoveToError(cfg.Failed.ErrorQueue);
                    });
            });

        [Handler]
        public class Handler : IHandleMessages<MessageToBeRetried>
        {
            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context) => throw new SimulatedException();
        }
    }

    public class MessageToBeRetried : IMessage;
}