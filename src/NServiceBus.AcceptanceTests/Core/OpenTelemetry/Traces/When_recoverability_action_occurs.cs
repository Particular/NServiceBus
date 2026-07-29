namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_recoverability_action_occurs : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_spans_for_all_recoverability_actions()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RecoverabilityEndpoint>(b => b
                .CustomConfig(c => c.Recoverability()
                    .Immediate(i => i.NumberOfRetries(1))
                    .Delayed(i => i.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1)))
                    .CustomPolicy((cfg, errorContext) =>
                        errorContext.Headers[Headers.EnclosedMessageTypes].Contains(nameof(DiscardMessage))
                            ? RecoverabilityAction.Discard("test discard reason")
                            : DefaultRecoverabilityPolicy.Invoke(cfg, errorContext)))
                .DoNotFailOnErrorMessages()
                .When(async s =>
                {
                    await s.SendLocal(new FailingMessage());
                    await s.SendLocal(new DiscardMessage());
                }))
            .Done(_ => ActionTags().Contains("move_to_error") && ActionTags().Contains("discard"))
            .Run();

        var activities = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities();

        var immediateRetry = activities.FirstOrDefault(a => (string)a.GetTagItem(ActivityTagName) == "immediate_retry");
        var delayedRetry = activities.FirstOrDefault(a => (string)a.GetTagItem(ActivityTagName) == "delayed_retry");
        var moveToError = activities.Single(a => (string)a.GetTagItem(ActivityTagName) == "move_to_error");
        var discard = activities.Single(a => (string)a.GetTagItem(ActivityTagName) == "discard");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(immediateRetry, Is.Not.Null, "expected at least one immediate retry span");
            Assert.That(immediateRetry?.DisplayName, Is.EqualTo("immediate retry"));

            Assert.That(delayedRetry, Is.Not.Null, "expected at least one delayed retry span");
            Assert.That(delayedRetry?.DisplayName, Is.EqualTo("delayed retry"));

            Assert.That(moveToError.DisplayName, Does.StartWith("move to "));
            Assert.That(discard.DisplayName, Is.EqualTo("discard"));
        }
    }

    [Test]
    public async Task Should_include_destination_in_display_name_when_opted_in()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RecoverabilityEndpoint>(b => b
                .CustomConfig(c =>
                {
                    c.Recoverability().Immediate(i => i.NumberOfRetries(1)).Delayed(i => i.NumberOfRetries(0));
                    c.Tracing().UseMessageDestinationInSpanNames = true;
                })
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(_ => ActionTags().Contains("move_to_error"))
            .Run();

        var immediateRetry = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities()
            .First(a => (string)a.GetTagItem(ActivityTagName) == "immediate_retry");

        var expectedEndpointName = Conventions.EndpointNamingConvention(typeof(RecoverabilityEndpoint));
        Assert.That(immediateRetry.DisplayName, Is.EqualTo($"immediate retry {expectedEndpointName}"));
    }

    string[] ActionTags() =>
        NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities()
            .Select(a => (string)a.GetTagItem(ActivityTagName))
            .ToArray();

    const string ActivityTagName = "nservicebus.recoverability_action";

    public class Context : ScenarioContext;

    public class RecoverabilityEndpoint : EndpointConfigurationBuilder
    {
        public RecoverabilityEndpoint()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(template, (c, _) => { }, metadata => { });
        }

        [Handler]
        public class FailingMessageHandler : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context) => throw new SimulatedException("always fails");
        }

        [Handler]
        public class DiscardMessageHandler : IHandleMessages<DiscardMessage>
        {
            public Task Handle(DiscardMessage message, IMessageHandlerContext context) => throw new SimulatedException("always fails");
        }
    }

    public class FailingMessage : IMessage;

    public class DiscardMessage : IMessage;
}
