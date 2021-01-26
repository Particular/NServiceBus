﻿namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_cross_q_transactional_message_is_moved_to_error_queue : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_dispatch_outgoing_messages()
        {
            Requires.CrossQueueTransactionSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithOutgoingMessages>(b => b.DoNotFailOnErrorMessages()
                    .When((session, c) => session.SendLocal(new InitiatingMessage
                    {
                        Id = c.TestRunId
                    }))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.IsFalse(context.OutgoingMessageSent, "Outgoing messages should not be sent");
            Assert.IsTrue(context.FailedMessages.Any());
        }

        class Context : ScenarioContext
        {
            public bool MessageMovedToErrorQueue { get; set; }
            public bool OutgoingMessageSent { get; set; }
        }

        class EndpointWithOutgoingMessages : EndpointConfigurationBuilder
        {
            public EndpointWithOutgoingMessages()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
                    config.Pipeline.Register(new ThrowingBehavior(), "Behavior that always throws");
                    config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public InitiatingHandler(Context context)
                {
                    testContext = context;
                }

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

                Context testContext;
            }
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            public EndpointWithFailingHandler()
            {
                EndpointSetup<DefaultServer>((config, context) => { config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy))); });
            }

            class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public Task Handle(InitiatingMessage message, IMessageHandlerContext context)
                {
                    throw new SimulatedException("message should be moved to the error queue");
                }
            }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>(config => config.LimitMessageProcessingConcurrencyTo(1));
            }

            class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public InitiatingMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    if (initiatingMessage.Id == testContext.TestRunId)
                    {
                        testContext.MessageMovedToErrorQueue = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class SubsequentMessageHandler : IHandleMessages<SubsequentMessage>
            {
                public SubsequentMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SubsequentMessage message, IMessageHandlerContext context)
                {
                    if (message.Id == testContext.TestRunId)
                    {
                        testContext.OutgoingMessageSent = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        class ThrowingBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
        {
            public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, CancellationToken, Task> next, CancellationToken token)
            {
                await next(context, token).ConfigureAwait(false);

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
}