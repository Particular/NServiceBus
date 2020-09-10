namespace NServiceBus.AcceptanceTests.Correlation
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_with_no_correlation_id : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_the_message_id_as_the_correlation_id()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<CorrelationEndpoint>(b => b.When(session => session.SendLocal(new MyRequest())))
                .Done(c => c.GotRequest)
                .Run();

            Assert.AreEqual(context.MessageIdReceived, context.CorrelationIdReceived, "Correlation id should match MessageId");
        }

        public class Context : ScenarioContext
        {
            public string MessageIdReceived { get; set; }
            public bool GotRequest { get; set; }
            public string CorrelationIdReceived { get; set; }
        }

        public class CorrelationEndpoint : EndpointConfigurationBuilder
        {
            public CorrelationEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyResponseHandler : IHandleMessages<MyRequest>
            {
                public MyResponseHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyRequest message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.CorrelationIdReceived = context.MessageHeaders[Headers.CorrelationId];
                    testContext.MessageIdReceived = context.MessageId;
                    testContext.GotRequest = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }


        public class MyRequest : IMessage
        {
        }
    }
}