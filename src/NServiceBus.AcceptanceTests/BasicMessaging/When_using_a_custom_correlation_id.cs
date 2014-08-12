namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NUnit.Framework;

    public class When_using_a_custom_correlation_id : NServiceBusAcceptanceTest
    {
        static string CorrelationId = "my_custom_correlation_id";
       
        [Test]
        public void Should_use_the_given_id_as_the_transport_level_correlation_id()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<CorrelationEndpoint>(b => b.Given(bus => bus.Send(Address.Local, CorrelationId, new MyRequest())))
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

            class GetValueOfIncomingCorrelationId : IMutateIncomingTransportMessages, IConfigureBus
            {
                public Context Context { get; set; }

                public void MutateIncoming(TransportMessage transportMessage)
                {
                    Context.CorrelationIdReceived = transportMessage.CorrelationId;
                }

                public void Customize(ConfigurationBuilder builder)
                {
                    builder.RegisterComponents(c => c.ConfigureComponent<GetValueOfIncomingCorrelationId>(DependencyLifecycle.InstancePerCall));
                }
            }

            public class MyResponseHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest response)
                {
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
