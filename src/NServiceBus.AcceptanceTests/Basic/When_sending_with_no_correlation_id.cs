namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_sending_with_no_correlation_id : NServiceBusAcceptanceTest
    {  
        [Test]
        public void Should_use_the_message_id_as_the_correlation_id()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<CorrelationEndpoint>(b => b.Given(bus => bus.SendLocal(new MyRequest())))
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

            class GetValueOfIncomingCorrelationId : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public void MutateIncoming(TransportMessage transportMessage)
                {
                    Context.CorrelationIdReceived = transportMessage.CorrelationId;
                    Context.MessageIdReceived = transportMessage.Id;
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<GetValueOfIncomingCorrelationId>(DependencyLifecycle.InstancePerCall));
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
