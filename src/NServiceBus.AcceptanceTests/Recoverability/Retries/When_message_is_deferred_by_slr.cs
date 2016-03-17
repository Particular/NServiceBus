namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_message_is_deferred_by_slr : NServiceBusAcceptanceTest
    {
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_not_send_outgoing_messages(TransportTransactionMode transactionMode)
        {
            await Scenario.Define<Context>(c =>
            {
                c.Id = Guid.NewGuid();
                c.TransactionMode = transactionMode;
            })
            .WithEndpoint<Endpoint>(b => b.DoNotFailOnErrorMessages()
                .When((session, context) => session.SendLocal(new InitiatingMessage
                {
                    Id = context.Id
                }))
            )
            .WithEndpoint<ErrorSpy>()
            .Done(c => c.MessageMovedToErrorQueue)
            .Repeat(r => r.For<AllDtcTransports>())
            .Should(c => Assert.IsFalse(c.OutgoingMessageSent, "Outgoing messages should not be sent"))
            .Run();
        }

        const string ErrorSpyQueueName = "error_spy_queue";

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool MessageMovedToErrorQueue { get; set; }
            public bool OutgoingMessageSent { get; set; }
            public TransportTransactionMode TransactionMode { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = (Context)context.ScenarioContext;

                    config.UseTransport(context.GetTransportType()).Transactions(testContext.TransactionMode);
                    config.DisableFeature<FirstLevelRetries>();
                    config.EnableFeature<SecondLevelRetries>();
                    config.EnableFeature<TimeoutManager>();
                    config.Pipeline.Register(new RegisterThrowingBehavior());
                    config.SendFailedMessagesTo(ErrorSpyQueueName);
                })
                .WithConfig<SecondLevelRetriesConfig>(slrConfig =>
                {
                    slrConfig.NumberOfRetries = 1;
                    slrConfig.TimeIncrease = TimeSpan.FromSeconds(1);
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public Context TestContext { get; set; }

                public async Task Handle(InitiatingMessage message, IMessageHandlerContext context)
                {
                    if (message.Id == TestContext.Id)
                    {
                        await context.Send(ErrorSpyQueueName, new SubsequentMessage { Id = message.Id });
                    }
                }
            }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>(config => config.LimitMessageProcessingConcurrencyTo(1))
                    .CustomEndpointName(ErrorSpyQueueName);
            }

            class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(InitiatingMessage message, IMessageHandlerContext context)
                {
                    if (message.Id == TestContext.Id)
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
                    if (message.Id == TestContext.Id)
                    {
                        TestContext.OutgoingMessageSent = true;
                    }

                    return Task.FromResult(0);
                }
            }
        }

        class RegisterThrowingBehavior : RegisterStep
        {
            public RegisterThrowingBehavior() : base("ThrowingBehavior", typeof(ThrowingBehavior), "Behavior that always throws")
            {
                InsertAfter("SecondLevelRetries");
            }
        }

        class ThrowingBehavior : Behavior<ITransportReceiveContext>
        {
            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                await next().ConfigureAwait(false);

                throw new SimulatedException();
            }
        }

        class InitiatingMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        class SubsequentMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}