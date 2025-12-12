namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_transactionscope_message_is_moved_to_error_queue : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_dispatch_outgoing_messages()
    {
        Requires.DtcSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithOutgoingMessages>(b => b.DoNotFailOnErrorMessages()
                .When((session, c) => session.SendLocal(new InitiatingMessage
                {
                    Id = c.TestRunId
                }))
            )
            .WithEndpoint<ErrorSpy>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.OutgoingMessageSent, Is.False, "Outgoing messages should not be sent");
            Assert.That(!context.FailedMessages.IsEmpty, Is.True, "There should be failed messages registered in this scenario");
        }
    }

    class Context : ScenarioContext
    {
        public bool OutgoingMessageSent { get; set; }
    }

    class EndpointWithOutgoingMessages : EndpointConfigurationBuilder
    {
        public EndpointWithOutgoingMessages() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.TransactionScope;
                config.Pipeline.Register(new ThrowingBehavior(), "Behavior that always throws");
                config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
            });

        class InitiatingHandler(Context testContext) : IHandleMessages<InitiatingMessage>
        {
            public async Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
            {
                if (initiatingMessage.Id == testContext.TestRunId)
                {
                    await context.Send(Conventions.EndpointNamingConvention(typeof(ErrorSpy)), new SubsequentMessage
                    {
                        Id = initiatingMessage.Id
                    });
                }
            }
        }
    }

    class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandler() => EndpointSetup<DefaultServer>((config, context) => { config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy))); });

        class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage message, IMessageHandlerContext context) => throw new SimulatedException("message should be moved to the error queue");
        }
    }

    class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>(config => config.LimitMessageProcessingConcurrencyTo(1));

        class InitiatingMessageHandler(Context testContext) : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
            {
                if (initiatingMessage.Id == testContext.TestRunId)
                {
                    testContext.MarkAsCompleted();
                }

                return Task.CompletedTask;
            }
        }

        class SubsequentMessageHandler(Context testContext) : IHandleMessages<SubsequentMessage>
        {
            public Task Handle(SubsequentMessage message, IMessageHandlerContext context)
            {
                if (message.Id == testContext.TestRunId)
                {
                    testContext.OutgoingMessageSent = true;
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