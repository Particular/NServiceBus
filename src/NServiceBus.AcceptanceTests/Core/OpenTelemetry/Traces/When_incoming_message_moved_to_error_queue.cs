namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EndpointTemplates;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_incoming_message_moved_to_error_queue : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_add_start_new_trace_header()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .When(s => s.SendLocal(new FailingMessage()))
                .DoNotFailOnErrorMessages())
            .WithEndpoint<ErrorSpy>()
            .Run();

        Assert.That(context.ErrorMessageHeaders[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    public class Context : ScenarioContext
    {
        public Dictionary<string, string> ErrorMessageHeaders { get; set; }
    }

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        static readonly string ErrorQueueAddress = Conventions.EndpointNamingConvention(typeof(ErrorSpy));

        public FailingEndpoint() => EndpointSetup<DefaultServer>(c => c.SendFailedMessagesTo(ErrorQueueAddress));

        [Handler]
        public class FailingMessageHandler() : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context) => throw new SimulatedException(ErrorMessage);
        }
    }

    class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register<ErrorMessageDetector>("Detect incoming error messages"));

        class ErrorMessageDetector(Context testContext) : Behavior<ITransportReceiveContext>
        {
            public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                testContext.ErrorMessageHeaders = context.Message.Headers;
                testContext.MarkAsCompleted();
                return next();
            }
        }
    }

    public class FailingMessage : IMessage;

    const string ErrorMessage = "oh no!";
}