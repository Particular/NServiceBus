﻿namespace NServiceBus.AcceptanceTests.Core.Sagas;

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

        Assert.That(context.Intent, Is.EqualTo(MessageIntent.Reply));
        Assert.That(context.CorrelationIdOnReply, Is.EqualTo(context.OriginalCorrelationId), "Correlation id should be preserved so that things like callbacks work properly");
    }

    public class Context : ScenarioContext
    {
        public MessageIntent Intent { get; set; }
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

            public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context)
            {
                testContext.OriginalCorrelationId = context.MessageHeaders[Headers.CorrelationId];

                return Task.CompletedTask;
            }

            public Task Handle(MessageThatWillCauseSagaToReplyToOriginator message, IMessageHandlerContext context)
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

            public Task Handle(MyReplyToOriginator message, IMessageHandlerContext context)
            {
                testContext.Intent = (MessageIntent)Enum.Parse(typeof(MessageIntent), context.MessageHeaders[Headers.MessageIntent]);
                testContext.CorrelationIdOnReply = context.MessageHeaders[Headers.CorrelationId];
                return Task.CompletedTask;
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