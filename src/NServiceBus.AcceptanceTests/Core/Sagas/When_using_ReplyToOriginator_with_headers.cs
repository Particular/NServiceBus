namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_ReplyToOriginator_with_headers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_headers()
        {
            var customHeaderValue = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new InitiateRequestingSaga
                {
                    CustomHeaderValue = customHeaderValue
                })))
                .Done(c => c.CustomHeaderOnReply != null)
                .Run();

            Assert.AreEqual(customHeaderValue.ToString(), context.CustomHeaderOnReply, "Header values should be forwarded");
        }

        public class Context : ScenarioContext
        {
            public string CustomHeaderOnReply { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                        config.LimitMessageProcessingConcurrencyTo(1) //to avoid race conditions with the start and second message
                );
            }

            public class ReplyingSaga : Saga<ReplyingSaga.ReplyingSagaData>,
                IAmStartedByMessages<InitiateRequestingSaga>
            {
                public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context)
                {
                    var customHeaders = new Dictionary<string, string> { { "CustomHeader", Data.CustomHeaderValue.ToString() } };
                    return ReplyToOriginator(context, new MyReplyToOriginator(), customHeaders);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ReplyingSagaData> mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.CustomHeaderValue)
                        .ToSaga(s => s.CustomHeaderValue);
                }

                public class ReplyingSagaData : ContainSagaData
                {
                    public virtual Guid CustomHeaderValue { get; set; }
                }
            }

            class MyReplyToOriginatorHandler : IHandleMessages<MyReplyToOriginator>
            {
                public MyReplyToOriginatorHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyReplyToOriginator message, IMessageHandlerContext context)
                {
                    testContext.CustomHeaderOnReply = context.MessageHeaders["CustomHeader"];
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class InitiateRequestingSaga : ICommand
        {
            public Guid CustomHeaderValue { get; set; }
        }

        public class MyReplyToOriginator : IMessage
        {
        }
    }
}