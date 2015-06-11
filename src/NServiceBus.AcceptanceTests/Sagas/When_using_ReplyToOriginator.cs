namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;

    public class When_using_ReplyToOriginator : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_set_Reply_as_messageintent()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new InitiateRequestingSaga())))
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
                EndpointSetup<DefaultServer>();
            }

            public class RequestingSaga : Saga<RequestingSaga.RequestingSagaData>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<AnotherRequest>
            {
                public Context Context { get; set; }

                public void Handle(InitiateRequestingSaga message)
                {
                    Data.CorrIdForResponse = Guid.NewGuid(); //wont be needed in the future

                    Bus.SendLocal(new AnotherRequest
                    {
                        SomeCorrelationId = Data.CorrIdForResponse //wont be needed in the future
                    });
                }

                public void Handle(AnotherRequest message)
                {
                    ReplyToOriginator(new MyReplyToOriginator());
                    MarkAsComplete();
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<RequestingSagaData> mapper)
                {
                    //if this line is un-commented the timeout and secondary handler tests will start to fail
                    // for more info and discussion see TBD
                    mapper.ConfigureMapping<AnotherRequest>(m => m.SomeCorrelationId).ToSaga(s => s.CorrIdForResponse);
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

                public void Handle(MyReplyToOriginator message)
                {
                    Context.Intent = (MessageIntentEnum)Enum.Parse(typeof(MessageIntentEnum), Bus.CurrentMessageContext.Headers[Headers.MessageIntent]);
                    Context.Done = true;
                }
            }
        }

        public class InitiateRequestingSaga : ICommand { }

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