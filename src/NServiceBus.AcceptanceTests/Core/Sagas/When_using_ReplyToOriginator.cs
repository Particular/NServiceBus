namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_ReplyToOriginator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_correlation_context()
        {
            var sagaCorrelationId = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new InitiateRequestingSaga
                {
                    SagaCorrelationId = sagaCorrelationId
                }))
                    .When(c => c.OriginalCorrelationId != null, session =>
                            // we're sending a new message here to make sure we get a new correlation id to make sure that the saga
                            // preserves the original one. This message can't be sent from a message handler as it would float the received
                            // correlation id into the outgoing message instead of assigning a new one.
                            session.SendLocal(new MessageThatWillCauseSagaToReplyToOriginator
                            {
                                SagaCorrelationId = sagaCorrelationId
                            }))
                )
                .Done(c => c.CorrelationIdOnReply != null)
                .Run();

            Assert.AreEqual(MessageIntentEnum.Reply, context.Intent);
            Assert.AreEqual(context.OriginalCorrelationId, context.CorrelationIdOnReply, "Correlation id should be preserved so that things like callbacks work properly");
        }

        public class Context : ScenarioContext
        {
            public MessageIntentEnum Intent { get; set; }
            public string CorrelationIdOnReply { get; set; }
            public string OriginalCorrelationId { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                        config.LimitMessageProcessingConcurrencyTo(1) //to avoid race conditions with the start and second message
                );
            }

            public class RequestingSaga : Saga<RequestingSaga.RequestingSagaData>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<MessageThatWillCauseSagaToReplyToOriginator>
            {
                public RequestingSaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.OriginalCorrelationId = context.MessageHeaders[Headers.CorrelationId];

                    return Task.FromResult(0);
                }

                public Task Handle(MessageThatWillCauseSagaToReplyToOriginator message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return ReplyToOriginator(context, new MyReplyToOriginator());
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestingSagaData> mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.SagaCorrelationId)
                        .ToSaga(s => s.SagaCorrelationId);
                    mapper.ConfigureMapping<MessageThatWillCauseSagaToReplyToOriginator>(m => m.SagaCorrelationId)
                        .ToSaga(s => s.SagaCorrelationId);
                }

                public class RequestingSagaData : ContainSagaData
                {
                    public virtual Guid SagaCorrelationId { get; set; }
                }

                Context testContext;
            }

            class MyReplyToOriginatorHandler : IHandleMessages<MyReplyToOriginator>
            {
                public MyReplyToOriginatorHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyReplyToOriginator message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.Intent = (MessageIntentEnum)Enum.Parse(typeof(MessageIntentEnum), context.MessageHeaders[Headers.MessageIntent]);
                    testContext.CorrelationIdOnReply = context.MessageHeaders[Headers.CorrelationId];
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class InitiateRequestingSaga : ICommand
        {
            public Guid SagaCorrelationId { get; set; }
        }

        public class MessageThatWillCauseSagaToReplyToOriginator : IMessage
        {
            public Guid SagaCorrelationId { get; set; }
        }

        public class MyReplyToOriginator : IMessage
        {
        }
    }
}