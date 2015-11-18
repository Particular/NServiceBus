namespace NServiceBus.AcceptanceTests.Correlation
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_with_no_correlation_id : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_the_message_id_as_the_correlation_id()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<CorrelationEndpoint>(b => b.When(bus => bus.SendLocal(new MyRequest())))
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
                public Context TestContext { get; set; }

                public Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    TestContext.CorrelationIdReceived = context.MessageHeaders[Headers.CorrelationId];
                    TestContext.MessageIdReceived = context.MessageId;
                    TestContext.GotRequest = true;

                    return Task.FromResult(0);
                }
            }
        }


        [Serializable]
        public class MyRequest : IMessage
        {
        }
    }
}
