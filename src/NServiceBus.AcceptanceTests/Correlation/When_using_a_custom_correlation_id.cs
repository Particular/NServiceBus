namespace NServiceBus.AcceptanceTests.Correlation
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_a_custom_correlation_id : NServiceBusAcceptanceTest
    {
        static string CorrelationId = "my_custom_correlation_id";
       
        [Test]
        public void Should_use_the_given_id_as_the_transport_level_correlation_id()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<CorrelationEndpoint>(b => b.Given(bus =>
                    {
                        var options = new SendLocalOptions();

                        options.SetCorrelationId(CorrelationId);

                        bus.SendLocal(new MessageWithCustomCorrelationId(),options);
                    }))
                    .Done(c => c.GotRequest)
                    .Run();

            Assert.AreEqual(CorrelationId, context.CorrelationIdReceived, "Correlation ids should match");
        }

        public class Context : ScenarioContext
        {
            public bool GotRequest { get; set; }

            public string CorrelationIdReceived { get; set; }
        }

        public class CorrelationEndpoint : EndpointConfigurationBuilder
        {
            public CorrelationEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SendMessageWithCorrelationHandler : IHandleMessages<MessageWithCustomCorrelationId>
            {
                public IBus Bus { get; set; }
                public Context Context { get; set; }

                public void Handle(MessageWithCustomCorrelationId message)
                {
                    Context.CorrelationIdReceived = Bus.CurrentMessageContext.Headers[Headers.CorrelationId];

                    Context.GotRequest = true;
                }
            }
        }

         [Serializable]
        public class MessageWithCustomCorrelationId : IMessage
        {
        }
    }
}
