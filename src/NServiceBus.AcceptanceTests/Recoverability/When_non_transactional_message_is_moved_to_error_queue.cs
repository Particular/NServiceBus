namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_non_transactional_message_is_moved_to_error_queue : NServiceBusAcceptanceTest
{
    static string ErrorSpyAddress => Conventions.EndpointNamingConvention(typeof(ErrorSpy));

    [Test]
    public async Task Should_dispatch_outgoing_messages()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithOutgoingMessages>(b => b.DoNotFailOnErrorMessages()
                .When((session, c) => session.SendLocal(new InitiatingMessage
                {
                    Id = c.TestRunId
                }))
            )
            .WithEndpoint<ErrorSpy>()
            .Run();

        Assert.That(!context.FailedMessages.IsEmpty, Is.True, "Messages should have failed");
    }

    public class Context : ScenarioContext
    {
        public bool MessageMovedToErrorQueue { get; set; }
        public bool OutgoingMessageSent { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(MessageMovedToErrorQueue, OutgoingMessageSent);
    }

    public class EndpointWithOutgoingMessages : EndpointConfigurationBuilder
    {
        public EndpointWithOutgoingMessages() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.None;
                config.Pipeline.Register(new ThrowingBehavior(), "Behavior that always throws");
                config.SendFailedMessagesTo(ErrorSpyAddress);
            });

        [Handler]
        public class InitiatingHandler(Context testContext) : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
            {
                if (initiatingMessage.Id == testContext.TestRunId)
                {
                    var message = new SubsequentMessage
                    {
                        Id = initiatingMessage.Id
                    };
                    return context.Send(ErrorSpyAddress, message);
                }
                return Task.CompletedTask;
            }
        }
    }

    public class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandler() => EndpointSetup<DefaultServer>((config, context) => { config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy))); });

        [Handler]
        public class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage message, IMessageHandlerContext context) => throw new SimulatedException("message should be moved to the error queue");
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>(config => config.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class InitiatingMessageHandler(Context testContext) : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
            {
                if (initiatingMessage.Id == testContext.TestRunId)
                {
                    testContext.MessageMovedToErrorQueue = true;
                    testContext.MaybeCompleted();
                }

                return Task.CompletedTask;
            }
        }

        [Handler]
        public class SubsequentMessageHandler(Context testContext) : IHandleMessages<SubsequentMessage>
        {
            public Task Handle(SubsequentMessage message, IMessageHandlerContext context)
            {
                if (message.Id == testContext.TestRunId)
                {
                    testContext.OutgoingMessageSent = true;
                    testContext.MaybeCompleted();
                }

                return Task.CompletedTask;
            }
        }
    }

    class ThrowingBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
    {
        public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
        {
            await next(context).ConfigureAwait(false);

            throw new SimulatedException();
        }
    }

    public class InitiatingMessage : IMessage
    {
        public Guid Id { get; set; }
    }

    public class SubsequentMessage : IMessage
    {
        public Guid Id { get; set; }
    }
}