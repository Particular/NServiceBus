namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Pipeline;
    using NServiceBus.Pipeline;
    using EndpointTemplates;

    public class When_using_receive_only_transaction_level : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_rollback_sent_messages()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceiveOnlyEndpoint>(e => e
                    .DoNotFailOnErrorMessages() // incoming message is expected to fail
                    .When((session, ctx) =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.SetMessageId(ctx.TestRunId.ToString());
                    return session.Send(new IncomingMessage(), sendOptions);
                }))
                .Done(c => c.ReceivedOutgoingMessage)
                .Run(TimeSpan.FromSeconds(15));

            Assert.IsTrue(context.ReceivedOutgoingMessage);
        }

        class Context : ScenarioContext
        {
            public bool ReceivedOutgoingMessage { get; set; }
        }

        class ReceiveOnlyEndpoint : EndpointConfigurationBuilder
        {
            public ReceiveOnlyEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register(typeof(FailAfterBatchedDispatchBehavior), "throws after outgoing messages have been dispatched");
                    c.UseTransport<MsmqTransport>()
                        .Transactions(TransportTransactionMode.ReceiveOnly);
                });
            }

            public class IncomingMessageHandler : IHandleMessages<IncomingMessage>
            {
                public Task Handle(IncomingMessage message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new OutgoingMessage
                    {
                        SentBy = context.MessageId
                    });
                }
            }

            public class OutgoingMessageHandler : IHandleMessages<OutgoingMessage>
            {
                Context testContext;

                public OutgoingMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
                {
                    if (message.SentBy == testContext.TestRunId.ToString())
                    {
                        testContext.ReceivedOutgoingMessage = true;
                    }
                    return Task.CompletedTask;
                }
            }

            public class FailAfterBatchedDispatchBehavior : Behavior<ITransportReceiveContext>
            {
                Context testContext;

                public FailAfterBatchedDispatchBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
                {
                    await next();

                    if (context.Message.MessageId == testContext.TestRunId.ToString())
                    {
                        // we throw after batched dispatch completed
                        throw new SimulatedException();
                    }
                }
            }
        }

        public class IncomingMessage : ICommand
        {
        }

        public class OutgoingMessage : ICommand
        {
            public string SentBy { get; set; }
        }
    }
}