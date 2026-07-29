namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_recoverability_action_occurs : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_span_for_immediate_retry()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RecoverabilityEndpoint>(b => b
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var activity = GetSingleRecoverabilityActivity();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.DisplayName, Is.EqualTo("immediate retry"));
            Assert.That(activity.GetTagItem(ActivityTagName), Is.EqualTo("immediate_retry"));
        }
    }

    [Test]
    public async Task Should_create_span_for_delayed_retry()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RecoverabilityEndpoint>(b => b
                .CustomConfig(c => c.Recoverability()
                    .Immediate(i => i.NumberOfRetries(0))
                    .Delayed(i => i.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1))))
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var activity = GetSingleRecoverabilityActivity();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.DisplayName, Is.EqualTo("delayed retry"));
            Assert.That(activity.GetTagItem(ActivityTagName), Is.EqualTo("delayed_retry"));
        }
    }

    [Test]
    public async Task Should_create_span_for_move_to_error()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RecoverabilityEndpoint>(b => b
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(0)).Delayed(i => i.NumberOfRetries(0)))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(_ => NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Count == 1)
            .Run();

        var activity = GetSingleRecoverabilityActivity();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.DisplayName, Does.StartWith("move to "));
            Assert.That(activity.GetTagItem(ActivityTagName), Is.EqualTo("move_to_error"));
        }
    }

    [Test]
    public async Task Should_create_span_for_discard()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RecoverabilityEndpoint>(b => b
                .CustomConfig(c => c.Recoverability().CustomPolicy((_, _) => RecoverabilityAction.Discard("test discard reason")))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(_ => NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Count == 1)
            .Run();

        var activity = GetSingleRecoverabilityActivity();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.DisplayName, Is.EqualTo("discard"));
            Assert.That(activity.GetTagItem(ActivityTagName), Is.EqualTo("discard"));
        }
    }

    [Test]
    public async Task Should_include_destination_in_display_name_when_opted_in()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RecoverabilityEndpoint>(b => b
                .CustomConfig(c =>
                {
                    c.Recoverability().Immediate(i => i.NumberOfRetries(1));
                    c.Tracing().UseMessageDestinationInSpanNames = true;
                })
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var activity = GetSingleRecoverabilityActivity();
        var expectedEndpointName = Conventions.EndpointNamingConvention(typeof(RecoverabilityEndpoint));
        Assert.That(activity.DisplayName, Is.EqualTo($"immediate retry {expectedEndpointName}"));
    }

    Activity GetSingleRecoverabilityActivity() =>
        NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Single();

    const string ActivityTagName = "nservicebus.recoverability_action";

    public class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
    }

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
        public class Handler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter == 1)
                {
                    throw new SimulatedException("first attempt fails");
                }

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class FailingMessage : IMessage;
}
