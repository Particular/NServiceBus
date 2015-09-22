namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_using_ReplyToOriginator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_set_Reply_as_messageintent()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocalAsync(new InitiateRequestingSaga { SomeCorrelationId = Guid.NewGuid() })))
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(MessageIntentEnum.Reply, context.Intent);
        }

        public class Context : ScenarioContext
        {
            public MessageIntentEnum Intent { get; set; }
            public bool Done { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {

            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class RequestingSaga : Saga<RequestingSaga.RequestingSagaData>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<AnotherRequest>
            {
                public Context Context { get; set; }

                public Task Handle(InitiateRequestingSaga message)
                {
                    Data.CorrIdForResponse = message.SomeCorrelationId; //wont be needed in the future

                    return Bus.SendLocalAsync(new AnotherRequest
                    {
                        SomeCorrelationId = Data.CorrIdForResponse //wont be needed in the future
                    });
                }

                public async Task Handle(AnotherRequest message)
                {
                    await ReplyToOriginatorAsync(new MyReplyToOriginator());
                    MarkAsComplete();
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestingSagaData> mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.SomeCorrelationId)
                        .ToSaga(s => s.CorrIdForResponse);
                    mapper.ConfigureMapping<AnotherRequest>(m => m.SomeCorrelationId)
                        .ToSaga(s => s.CorrIdForResponse);
                }

                public class RequestingSagaData : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; } //wont be needed in the future
                }
            }

            class MyReplyToOriginatorHandler : IHandleMessages<MyReplyToOriginator>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(MyReplyToOriginator message)
                {
                    Context.Intent = (MessageIntentEnum)Enum.Parse(typeof(MessageIntentEnum), Bus.CurrentMessageContext.Headers[Headers.MessageIntent]);
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class InitiateRequestingSaga : ICommand
        {
            public Guid SomeCorrelationId { get; set; }
        }

        public class AnotherRequest : ICommand
        {
            public Guid SomeCorrelationId { get; set; }
        }

        public class MyReplyToOriginator : IMessage
        {
            public Guid SomeCorrelationId { get; set; }
        }
    }
}