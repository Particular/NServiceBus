namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
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
            .WithEndpoint<ImmediateRetryEndpoint>(b => b
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var recoverabilityActivities = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities();
        Assert.That(recoverabilityActivities, Has.Count.EqualTo(1), "only the first (failing) attempt triggers a recoverability decision");

        var activity = recoverabilityActivities.Single();
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
            .WithEndpoint<DelayedRetryEndpoint>(b => b
                .CustomConfig(c => c.Recoverability().Delayed(i => i.NumberOfRetries(1).TimeIncrease(TimeSpan.FromMilliseconds(1))))
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var recoverabilityActivities = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities();
        Assert.That(recoverabilityActivities, Has.Count.EqualTo(1), "only the first (failing) attempt triggers a recoverability decision");

        var activity = recoverabilityActivities.Single();
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
            .WithEndpoint<AlwaysFailingEndpoint>(b => b
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(0)).Delayed(i => i.NumberOfRetries(0)))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .WithEndpoint<ErrorSpy>()
            .Run();

        var recoverabilityActivities = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities();
        Assert.That(recoverabilityActivities, Has.Count.EqualTo(1));

        var activity = recoverabilityActivities.Single();
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
            .WithEndpoint<DiscardingEndpoint>(b => b
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var recoverabilityActivities = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities();
        Assert.That(recoverabilityActivities, Has.Count.EqualTo(1));

        var activity = recoverabilityActivities.Single();
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
            .WithEndpoint<ImmediateRetryEndpointWithDestinationNaming>(b => b
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var recoverabilityActivities = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities();
        var activity = recoverabilityActivities.Single();

        var expectedEndpointName = Conventions.EndpointNamingConvention(typeof(ImmediateRetryEndpointWithDestinationNaming));
        Assert.That(activity.DisplayName, Is.EqualTo($"immediate retry {expectedEndpointName}"));
    }

    const string ActivityTagName = "nservicebus.recoverability_action";

    public class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
    }

    public class ImmediateRetryEndpoint : EndpointConfigurationBuilder
    {
        public ImmediateRetryEndpoint() => EndpointSetup<DefaultServer>();

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

    public class ImmediateRetryEndpointWithDestinationNaming : EndpointConfigurationBuilder
    {
        public ImmediateRetryEndpointWithDestinationNaming() =>
            EndpointSetup<DefaultServer>(b => b.Tracing().UseMessageDestinationInSpanNames = true);

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

    public class DelayedRetryEndpoint : EndpointConfigurationBuilder
    {
        public DelayedRetryEndpoint()
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

    public class AlwaysFailingEndpoint : EndpointConfigurationBuilder
    {
        static readonly string ErrorQueueAddress = Conventions.EndpointNamingConvention(typeof(ErrorSpy));

        public AlwaysFailingEndpoint() => EndpointSetup<DefaultServer>(c => c.SendFailedMessagesTo(ErrorQueueAddress));

        [Handler]
        public class Handler : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context) => throw new SimulatedException("always fails");
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>();

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class DiscardingEndpoint : EndpointConfigurationBuilder
    {
        public DiscardingEndpoint() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                var testContext = (Context)context.ScenarioContext;
                config.Recoverability().CustomPolicy((_, _) =>
                {
                    var action = RecoverabilityAction.Discard("test discard reason");
                    testContext.MarkAsCompleted();
                    return action;
                });
            });

        [Handler]
        public class Handler : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context) => throw new SimulatedException("always fails");
        }
    }

    public class FailingMessage : IMessage;
}
