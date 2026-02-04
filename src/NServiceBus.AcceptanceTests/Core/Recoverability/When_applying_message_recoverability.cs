namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_applying_message_recoverability : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_allow_for_alternate_move_to_error_action()
    {
        var onMessageSentToErrorQueueTriggered = false;
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithFailingHandler>(b => b
                .DoNotFailOnErrorMessages()
                .CustomConfig(config =>
                {
                    config.Recoverability()
                    .Failed(f => f.OnMessageSentToErrorQueue((_, __) =>
                    {
                        onMessageSentToErrorQueueTriggered = true;
                        return Task.CompletedTask;
                    }));
                })
                .When((session, ctx) => session.SendLocal(new InitiatingMessage()))
            )
            .WithEndpoint<ErrorSpy>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.MessageBodyWasEmpty, Is.True);
            Assert.That(onMessageSentToErrorQueueTriggered, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool MessageBodyWasEmpty { get; set; }
    }

    public class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        static readonly string ErrorQueueAddress = Conventions.EndpointNamingConvention(typeof(ErrorSpy));

        public EndpointWithFailingHandler() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                config.SendFailedMessagesTo(ErrorQueueAddress);
                config.Pipeline.Register(typeof(CustomRecoverabilityActionBehavior), "Applies a custom recoverability actions");
            });

        public class CustomRecoverabilityActionBehavior : Behavior<IRecoverabilityContext>
        {
            public override Task Invoke(IRecoverabilityContext context, Func<Task> next)
            {
                if (context.RecoverabilityAction is MoveToError)
                {
                    //Here we could store the body, headers and error metadata elsewhere

                    context.RecoverabilityAction = new CustomOnErrorAction(context.RecoverabilityConfiguration.Failed.ErrorQueue);
                }

                return next();
            }

            class CustomOnErrorAction(string errorQueue) : MoveToError(errorQueue)
            {
                public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
                {
                    var routingContexts = base.GetRoutingContexts(context);

                    // show how we just send an empty message with the message id to the error queue
                    // headers are preserved to make sure the necessary acceptance test infrastructure is still present
                    foreach (var routingContext in routingContexts)
                    {
                        routingContext.Message.UpdateBody(ReadOnlyMemory<byte>.Empty);
                    }

                    return routingContexts;
                }
            }
        }

        [Handler]
        public class InitiatingHandler : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context) => throw new SimulatedException("Some failure");
        }
    }

    class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register(typeof(ErrorMessageDetector), "Detect incoming error messages"));

        class ErrorMessageDetector(Context testContext) : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
            {
                testContext.MessageBodyWasEmpty = context.Message.Body.IsEmpty;
                testContext.MarkAsCompleted();
                return next(context);
            }
        }
    }

    public class InitiatingMessage : IMessage;
}