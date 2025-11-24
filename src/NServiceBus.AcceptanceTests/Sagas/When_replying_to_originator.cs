namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_replying_to_originator : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_route_the_message_to_the_endpoint_starting_the_saga()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new InitiateRequestingSaga())))
            .Done(c => c.DidRequestingSagaGetTheResponse)
            .Run();

        Assert.That(context.DidRequestingSagaGetTheResponse, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool DidRequestingSagaGetTheResponse { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        public class RequestResponseRequestingSaga2(Context testContext)
            : Saga<RequestResponseRequestingSaga2.RequestResponseRequestingSagaData2>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<ResponseFromOtherSaga>
        {
            public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context) =>
                context.SendLocal(new RequestToRespondingSaga
                {
                    SomeIdThatTheResponseSagaCanCorrelateBackToUs = Data.CorrIdForResponse
                });

            public Task Handle(ResponseFromOtherSaga message, IMessageHandlerContext context)
            {
                testContext.DidRequestingSagaGetTheResponse = true;

                MarkAsComplete();

                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRequestingSagaData2> mapper) =>
                mapper.MapSaga(s => s.CorrIdForResponse)
                    .ToMessage<InitiateRequestingSaga>(m => m.Id)
                    .ToMessage<ResponseFromOtherSaga>(m => m.SomeCorrelationId);

            public class RequestResponseRequestingSagaData2 : ContainSagaData
            {
                public virtual Guid CorrIdForResponse { get; set; }
            }
        }

        public class RequestResponseRespondingSaga2 : Saga<RequestResponseRespondingSaga2.RequestResponseRespondingSagaData2>,
            IAmStartedByMessages<RequestToRespondingSaga>,
            IHandleMessages<SendReplyFromNonInitiatingHandler>
        {
            public Task Handle(RequestToRespondingSaga message, IMessageHandlerContext context) =>
                context.SendLocal(new SendReplyFromNonInitiatingHandler
                {
                    SagaIdSoWeCanCorrelate = Data.CorrIdForRequest
                });

            public Task Handle(SendReplyFromNonInitiatingHandler message, IMessageHandlerContext context) =>
                //reply to originator must be used here since the sender of the incoming message is this saga and not the requesting saga
                ReplyToOriginator(context, new ResponseFromOtherSaga
                {
                    SomeCorrelationId = Data.CorrIdForRequest
                });

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRespondingSagaData2> mapper) =>
                mapper.MapSaga(s => s.CorrIdForRequest)
                    .ToMessage<RequestToRespondingSaga>(m => m.SomeIdThatTheResponseSagaCanCorrelateBackToUs)
                    .ToMessage<SendReplyFromNonInitiatingHandler>(m => m.SagaIdSoWeCanCorrelate);

            public class RequestResponseRespondingSagaData2 : ContainSagaData
            {
                public virtual Guid CorrIdForRequest { get; set; }
            }
        }
    }

    public class InitiateRequestingSaga : ICommand
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public class RequestToRespondingSaga : ICommand
    {
        public Guid SomeIdThatTheResponseSagaCanCorrelateBackToUs { get; set; }
    }

    public class ResponseFromOtherSaga : IMessage
    {
        public Guid SomeCorrelationId { get; set; }
    }

    public class SendReplyFromNonInitiatingHandler : ICommand
    {
        public Guid SagaIdSoWeCanCorrelate { get; set; }
    }
}