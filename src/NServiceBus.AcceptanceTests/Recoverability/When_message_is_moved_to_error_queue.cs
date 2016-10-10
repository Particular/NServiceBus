namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_message_is_moved_to_error_queue : NServiceBusAcceptanceTest
    {
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public Task Should_not_send_outgoing_messages(TransportTransactionMode transactionMode)
        {
            return Scenario.Define<Context>(c => { c.TransactionMode = transactionMode; })
                .WithEndpoint<EndpointWithOutgoingMessages>(b => b.DoNotFailOnErrorMessages()
                    .When((session, context) => session.SendLocal(new InitiatingMessage
                    {
                        Id = context.TestRunId
                    }))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Repeat(r => r.For<AllDtcTransports>())
                .Should(c => Assert.IsFalse(c.OutgoingMessageSent, "Outgoing messages should not be sent"))
                .Run();
        }

        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.None)]
        public Task May_send_outgoing_messages(TransportTransactionMode transactionMode)
        {
            return Scenario.Define<Context>(c => { c.TransactionMode = transactionMode; })
                .WithEndpoint<EndpointWithOutgoingMessages>(b => b.DoNotFailOnErrorMessages()
                    .When((session, context) => session.SendLocal(new InitiatingMessage
                    {
                        Id = context.TestRunId
                    }))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Repeat(r => r.For<AllDtcTransports>())
                .Run();
        }

        [Test]
        public async Task Should_log_exception()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithOutgoingMessages>(b => b
                    .DoNotFailOnErrorMessages()
                    .When((session, ctx) => session.SendLocal(new InitiatingMessage
                    {
                        Id = ctx.TestRunId
                    }))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.That(context.Logs, Has.Some.Message.Match($"Moving message .+ to the error queue '{ Conventions.EndpointNamingConvention(typeof(ErrorSpy)) }' because processing failed due to an exception: NServiceBus.AcceptanceTesting.SimulatedException:"));
        }

        class Context : ScenarioContext
        {
            public bool MessageMovedToErrorQueue { get; set; }
            public bool OutgoingMessageSent { get; set; }
            public TransportTransactionMode TransactionMode { get; set; }
        }

        class EndpointWithOutgoingMessages : EndpointConfigurationBuilder
        {
            public EndpointWithOutgoingMessages()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = context.ScenarioContext as Context;

                    config.UseTransport(context.GetTransportType())
                        .Transactions(testContext.TransactionMode);
                    config.Pipeline.Register(new ThrowingBehavior(), "Behavior that always throws");
                    config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public Context TestContext { get; set; }

                public async Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    if (initiatingMessage.Id == TestContext.TestRunId)
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
                public Context TestContext { get; set; }

                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    if (initiatingMessage.Id == TestContext.TestRunId)
                    {
                        TestContext.MessageMovedToErrorQueue = true;
                    }

                    return Task.FromResult(0);
                }
            }

            class SubsequentMessageHandler : IHandleMessages<SubsequentMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(SubsequentMessage message, IMessageHandlerContext context)
                {
                    if (message.Id == TestContext.TestRunId)
                    {
                        TestContext.OutgoingMessageSent = true;
                    }

                    return Task.FromResult(0);
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
}