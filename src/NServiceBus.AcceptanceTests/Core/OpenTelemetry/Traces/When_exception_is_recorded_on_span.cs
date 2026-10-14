namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_exception_is_recorded_on_span : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_not_add_recording_state_to_error_message_headers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .WithEndpoint<ErrorSpy>()
            .Run();

        Assert.That(context.ErrorMessageHeaders.Keys, Has.None.StartsWith(ExceptionInfoDataPrefix + "otel."),
            "span recording state is not exception data and should not reach the error queue");
    }

    [Test]
    public async Task Should_record_exception_on_each_attempt_when_the_same_instance_is_thrown()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<ReusedExceptionEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(2)))
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var handlerActivities = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities();

        Assert.That(handlerActivities, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(handlerActivities[0].Events, Has.Exactly(1).Items, "first attempt should record the exception");
            Assert.That(handlerActivities[1].Events, Has.Exactly(1).Items, "second attempt should record the same exception instance again");
        }
    }

    const string ExceptionInfoDataPrefix = "NServiceBus.ExceptionInfo.Data.";

    public class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
        public Dictionary<string, string> ErrorMessageHeaders { get; set; }
    }

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        static readonly string ErrorQueueAddress = Conventions.EndpointNamingConvention(typeof(ErrorSpy));

        public FailingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.SendFailedMessagesTo(ErrorQueueAddress);
            c.Recoverability().Immediate(i => i.NumberOfRetries(0)).Delayed(d => d.NumberOfRetries(0));
        });

        [Handler]
        public class FailingMessageHandler : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context) => throw new SimulatedException("always fails");
        }
    }

    public class ReusedExceptionEndpoint : EndpointConfigurationBuilder
    {
        public ReusedExceptionEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter <= 2)
                {
                    throw ReusedException;
                }

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            static readonly SimulatedException ReusedException = new("same instance on every attempt");
        }
    }

    class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register<ErrorMessageDetector>("Detect incoming error messages"));

        class ErrorMessageDetector(Context testContext) : Behavior<ITransportReceiveContext>
        {
            public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                testContext.ErrorMessageHeaders = context.Message.Headers.ToDictionary(x => x.Key, x => x.Value);
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class FailingMessage : IMessage;
}
