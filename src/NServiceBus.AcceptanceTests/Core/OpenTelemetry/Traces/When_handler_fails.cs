namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_handler_fails : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_be_able_to_use_custom_span()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.HandlerInvoked).Run();

        Assert.That(context.FailedMessages, Has.Count.EqualTo(1), "the message should have failed");

        Activity failedHandlerActivity = NServicebusActivityListener.CompletedActivities.GetInvokedHandlerActivities().Single();
        Assert.Multiple(() =>
        {
            Assert.That(failedHandlerActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(failedHandlerActivity.StatusDescription, Is.EqualTo(ErrorMessage));
        });

        var handlerActivityTags = failedHandlerActivity.Tags.ToImmutableDictionary();

        handlerActivityTags.VerifyTag("mytag", "myvalue");
    }

    class Context : ScenarioContext
    {
        public bool HandlerInvoked { get; set; }
    }

    class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>(c => c.Pipeline.Register(typeof(CustomHandlerSpanBehavior), "Adds a customer handler span"));

        class FailingMessageHandler(Context textContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                textContext.HandlerInvoked = true;
                throw new SimulatedException(ErrorMessage);
            }
        }

        class CustomHandlerSpanBehavior : Behavior<IInvokeHandlerContext>
        {
            static ActivitySource source = new ActivitySource("NServiceBus.Core", "0.1.0");

            public override async Task Invoke(IInvokeHandlerContext context, Func<Task> next)
            {
                var captured = Activity.Current;

                using var activity = source.StartActivity("NServiceBus.Diagnostics.InvokeHandler");

                // clear ambient activity to prevent the default nservicebus span to be created
                Activity.Current = null;

                activity!.DisplayName = context.MessageHandler.HandlerType.Name;
                activity.AddTag("nservicebus.handler.handler_type", context.MessageHandler.HandlerType.FullName);

                try
                {
                    await next();

                    activity.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.SetTag("mytag", "myvalue");
                    throw;
                }
                finally
                {
                    // restore ambient activity
                    Activity.Current = captured;
                }
            }
        }
    }

    public class FailingMessage : IMessage
    {
    }

    const string ErrorMessage = "oh no!";
}