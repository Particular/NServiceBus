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
                    .WithEndpoint<CorrelationEndpoint>(b => b.Given(bus =>
                    {
                        bus.SendLocal(new MyRequest());
                        return Task.FromResult(0);
                    }))
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
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest response)
                {
                    Context.CorrelationIdReceived = Bus.CurrentMessageContext.Headers[Headers.CorrelationId];
                    Context.MessageIdReceived = Bus.CurrentMessageContext.Id;
                    Context.GotRequest = true;
                }
            }
        }


        [Serializable]
        public class MyRequest : IMessage
        {
        }
    }
}
