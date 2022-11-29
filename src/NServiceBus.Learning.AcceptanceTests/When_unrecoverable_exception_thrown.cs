namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_unrecoverable_exception_thrown : NServiceBusAcceptanceTest
    {
        static string ErrorSpyAddress => Conventions.EndpointNamingConvention(typeof(ErrorSpy));

        [Test]
        public async Task Should_move_to_error_queue_without_retries()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithOutgoingMessages>(b => b.DoNotFailOnErrorMessages()
                    .When((session, c) => session.SendLocal(new MessageOne
                    {
                        Id = c.TestRunId
                    }))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.AreEqual(1, context.NrOfTimesHandlerWasInvoked);
        }

        [Test]
        public async Task Should_move_to_error_queue_without_retries2()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithOutgoingMessages>(b => b.DoNotFailOnErrorMessages()
                    .When((session, c) => session.SendLocal(new MessageTwo()
                    {
                        Id = c.TestRunId
                    }))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.AreEqual(1, context.NrOfTimesHandlerWasInvoked);
        }

        class Context : ScenarioContext
        {
            public bool MessageMovedToErrorQueue { get; set; }
            public int NrOfTimesHandlerWasInvoked { get; set; }
        }

        class EndpointWithOutgoingMessages : EndpointConfigurationBuilder
        {
            public EndpointWithOutgoingMessages()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.None;
                    config.SendFailedMessagesTo(ErrorSpyAddress);
                });
            }

            class MessageOneHandler : IHandleMessages<MessageOne>
            {
                public MessageOneHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageOne message, IMessageHandlerContext context)
                {
                    testContext.NrOfTimesHandlerWasInvoked++;
                    throw new UnrecoverableException("This exception is unrecoverable");
                }

                Context testContext;
            }

            class MessageTwoHandler : IHandleMessages<MessageTwo>
            {
                public MessageTwoHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageTwo initiatingMessage, IMessageHandlerContext context)
                {
                    testContext.NrOfTimesHandlerWasInvoked++;
                    throw new SpecificException("This exception is unrecoverable");
                }

                Context testContext;
            }

            class SpecificException : UnrecoverableException
            {
                public SpecificException(string message) : base(message)
                {
                }

                public SpecificException(string message, Exception innerException) : base(message, innerException)
                {
                }
            }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>(config => config.LimitMessageProcessingConcurrencyTo(1));
            }

            class MessageOneHandler : IHandleMessages<MessageOne>
            {
                public MessageOneHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageOne message, IMessageHandlerContext context)
                {
                    if (message.Id == testContext.TestRunId)
                    {
                        testContext.MessageMovedToErrorQueue = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class MessageTwoHandler : IHandleMessages<MessageTwo>
            {
                public MessageTwoHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageTwo message, IMessageHandlerContext context)
                {
                    if (message.Id == testContext.TestRunId)
                    {
                        testContext.MessageMovedToErrorQueue = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageOne : IMessage
        {
            public Guid Id { get; set; }
        }

        public class MessageTwo : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}