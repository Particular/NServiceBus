namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_doing_request_response_between_sagas : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_autocorrelate_the_response_back_to_the_requesting_saga()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new InitiateRequestingSaga())))
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

        [Saga]
        public class RequestResponseRequestingSaga1(Context testContext)
            : Saga<RequestResponseRequestingSaga1.RequestResponseRequestingSagaData1>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<ResponseFromOtherSaga>
        {
            public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context) =>
                context.SendLocal(new RequestToRespondingSaga
                {
                    SomeId = Guid.NewGuid()
                });

            public Task Handle(ResponseFromOtherSaga message, IMessageHandlerContext context)
            {
                testContext.DidRequestingSagaGetTheResponse = true;
                MarkAsComplete();
                testContext.MarkAsCompleted();

                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRequestingSagaData1> mapper) =>
                mapper.MapSaga(s => s.CorrIdForResponse)
                    .ToMessage<InitiateRequestingSaga>(m => m.Id)
                    .ToMessage<ResponseFromOtherSaga>(m => m.SomeCorrelationId);

            public class RequestResponseRequestingSagaData1 : ContainSagaData
            {
                public virtual Guid CorrIdForResponse { get; set; }
            }
        }

        [Saga]
        public class RequestResponseRespondingSaga1 : Saga<RequestResponseRespondingSaga1.RequestResponseRespondingSagaData1>,
            IAmStartedByMessages<RequestToRespondingSaga>
        {
            public Task Handle(RequestToRespondingSaga message, IMessageHandlerContext context) =>
                // Both reply and reply to originator work here since the sender of the incoming message is the requesting saga
                // we explicitly set the correlation ID to a non-existent saga since auto correlation happens to work for this special case
                // where we reply from the first handler
                context.Reply(new ResponseFromOtherSaga { SomeCorrelationId = Guid.NewGuid() });

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestResponseRespondingSagaData1> mapper) =>
                mapper.MapSaga(s => s.CorrIdForRequest)
                    .ToMessage<RequestToRespondingSaga>(m => m.SomeId);

            public class RequestResponseRespondingSagaData1 : ContainSagaData
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
        public Guid SomeId { get; set; }
    }

    public class ResponseFromOtherSaga : IMessage
    {
        public Guid SomeCorrelationId { get; set; }
    }
}