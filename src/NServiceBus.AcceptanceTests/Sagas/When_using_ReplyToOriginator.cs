namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_using_ReplyToOriginator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_correlation_context()
        {
            var messageId = Guid.NewGuid().ToString();
            var someId = Guid.NewGuid();
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session =>
                    {
                        var option = new SendOptions();

                        option.SetMessageId(messageId);
                        option.RouteToThisEndpoint();

                        return session.Send(new InitiateRequestingSaga
                        {
                            SomeId = someId
                        }, option);
                    })
                    .When(c => c.GotFirstMessage, session =>
                        //we're sending a new message here to make sure we get a new correlation id to make sure that the saga 
                        //preserves the original one. If not the fact that we flow correlation ids from incoming messages to outgoing will
                        // hide potential bugs
                        session.SendLocal(new MessageThatWillCauseSagaToReplyToOriginator
                        {
                            SomeId = someId
                        }))
                )
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(MessageIntentEnum.Reply, context.Intent);
            Assert.AreEqual(messageId, context.ReceivedCorrelationId, "Message id should be preserved and used as correlation id on reply so that things like callbacks work properly");
        }

        public class Context : ScenarioContext
        {
            public MessageIntentEnum Intent { get; set; }
            public string ReceivedCorrelationId { get; set; }
            public bool Done { get; set; }
            public bool GotFirstMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.LimitMessageProcessingConcurrencyTo(1); //to avoid race conditions with the start and second message
                });
            }

            public class RequestingSaga : Saga<RequestingSaga.RequestingSagaData>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<MessageThatWillCauseSagaToReplyToOriginator>
            {
                public Context TestContext { get; set; }

                public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context)
                {
                    Data.CorrIdForResponse = message.SomeId; //wont be needed in the future
                    TestContext.GotFirstMessage = true;

                    return Task.FromResult(0);
                }

                public async Task Handle(MessageThatWillCauseSagaToReplyToOriginator message, IMessageHandlerContext context)
                {
                    await ReplyToOriginator(context, new MyReplyToOriginator());
                    MarkAsComplete();
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestingSagaData> mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.SomeId)
                        .ToSaga(s => s.CorrIdForResponse);
                    mapper.ConfigureMapping<MessageThatWillCauseSagaToReplyToOriginator>(m => m.SomeId)
                        .ToSaga(s => s.CorrIdForResponse);
                }

                public class RequestingSagaData : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; } //wont be needed in the future
                }
            }

            class MyReplyToOriginatorHandler : IHandleMessages<MyReplyToOriginator>
            {
                public Context TestContext { get; set; }

                public Task Handle(MyReplyToOriginator message, IMessageHandlerContext context)
                {
                    TestContext.Intent = (MessageIntentEnum)Enum.Parse(typeof(MessageIntentEnum), context.MessageHeaders[Headers.MessageIntent]);
                    TestContext.ReceivedCorrelationId = context.MessageHeaders[Headers.CorrelationId];
                    TestContext.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class InitiateRequestingSaga : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class MessageThatWillCauseSagaToReplyToOriginator : IMessage
        {
            public Guid SomeId { get; set; }
        }

        public class MyReplyToOriginator : IMessage
        {
        }
    }
}