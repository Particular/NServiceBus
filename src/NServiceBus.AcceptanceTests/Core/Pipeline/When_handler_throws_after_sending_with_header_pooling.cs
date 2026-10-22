namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_handler_throws_after_sending_with_header_pooling : NServiceBusAcceptanceTest
{
    static string ErrorSpyAddress => Conventions.EndpointNamingConvention(typeof(ErrorSpy));

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Should_keep_outgoing_headers_readable_until_the_receive_pipeline_completes(bool enableHeaderPooling, bool immediateDispatch)
    {
        var context = await Scenario.Define<Context>(c => c.ImmediateDispatch = immediateDispatch)
            .WithEndpoint<FailingEndpoint>(b => b
                .DoNotFailOnErrorMessages()
                .CustomConfig(c =>
                {
                    if (enableHeaderPooling)
                    {
                        c.EnableHeaderPooling();
                    }
                })
                .When(session => session.SendLocal(new TriggerMessage())))
            .WithEndpoint<ErrorSpy>()
            .Done(c => c.MovedToErrorQueue && (!immediateDispatch || c.FollowUpReceived))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ExceptionReadingHeadersWhileUnwinding, Is.Null, $"Reading the outgoing headers while the receive pipeline unwound threw: {context.ExceptionReadingHeadersWhileUnwinding}");
            Assert.That(context.FollowUpMessageIdWhileUnwinding, Is.EqualTo(context.FollowUpMessageId), "Outgoing headers stay readable until the receive pipeline that sent them completes, including when it fails.");
            Assert.That(context.FollowUpReceived, Is.EqualTo(immediateDispatch), "A batched send is discarded when the handler fails; an immediate send is delivered.");
            if (immediateDispatch)
            {
                Assert.That(context.FollowUpMessageIdReceived, Is.EqualTo(context.FollowUpMessageId));
            }
        }
    }

    public class Context : ScenarioContext
    {
        public bool ImmediateDispatch { get; set; }
        public Dictionary<string, string> FollowUpHeaders { get; set; }
        public string FollowUpMessageId { get; set; }
        public string FollowUpMessageIdWhileUnwinding { get; set; }
        public Exception ExceptionReadingHeadersWhileUnwinding { get; set; }
        public bool MovedToErrorQueue { get; set; }
        public bool FollowUpReceived { get; set; }
        public string FollowUpMessageIdReceived { get; set; }
    }

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.SendFailedMessagesTo(ErrorSpyAddress);
            c.Pipeline.Register(b => new CaptureOutgoingHeadersBehavior(b.GetService<Context>()), "Captures the outgoing headers of the follow-up message");
            c.Pipeline.Register(b => new ReadHeadersWhileUnwindingBehavior(b.GetService<Context>()), "Reads the captured headers while the failed receive pipeline unwinds");
        });

        public class CaptureOutgoingHeadersBehavior(Context testContext) : Behavior<IOutgoingLogicalMessageContext>
        {
            public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                if (context.Message.MessageType == typeof(FollowUpMessage))
                {
                    testContext.FollowUpHeaders = context.Headers;
                    testContext.FollowUpMessageId = context.MessageId;
                }

                return next();
            }
        }

        public class ReadHeadersWhileUnwindingBehavior(Context testContext) : Behavior<ITransportReceiveContext>
        {
            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                try
                {
                    await next();
                }
                catch (SimulatedException)
                {
                    try
                    {
                        testContext.FollowUpMessageIdWhileUnwinding = testContext.FollowUpHeaders[Headers.MessageId];
                    }
                    catch (KeyNotFoundException ex)
                    {
                        testContext.ExceptionReadingHeadersWhileUnwinding = ex;
                    }

                    throw;
                }
            }
        }

        [Handler]
        public class TriggerMessageHandler(Context testContext) : IHandleMessages<TriggerMessage>
        {
            public async Task Handle(TriggerMessage message, IMessageHandlerContext context)
            {
                var options = new SendOptions();
                options.SetDestination(ErrorSpyAddress);
                if (testContext.ImmediateDispatch)
                {
                    options.RequireImmediateDispatch();
                }

                await context.Send(new FollowUpMessage(), options);

                throw new SimulatedException("handler failed after sending");
            }
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class TriggerMessageHandler(Context testContext) : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context)
            {
                testContext.MovedToErrorQueue = true;
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class FollowUpMessageHandler(Context testContext) : IHandleMessages<FollowUpMessage>
        {
            public Task Handle(FollowUpMessage message, IMessageHandlerContext context)
            {
                testContext.FollowUpMessageIdReceived = context.MessageId;
                testContext.FollowUpReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class TriggerMessage : ICommand;

    public class FollowUpMessage : ICommand;
}
