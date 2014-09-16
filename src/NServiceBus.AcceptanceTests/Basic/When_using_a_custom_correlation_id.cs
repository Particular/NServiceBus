namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_using_a_custom_correlation_id : NServiceBusAcceptanceTest
    {
        static string CorrelationId = "my_custom_correlation_id";
       
        [Test]
        public void Should_use_the_given_id_as_the_transport_level_correlation_id()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<CorrelationEndpoint>(b => b.Given(bus => bus.SendLocal(new SendMessageWithCorrelation())))
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

            class GetValueOfIncomingCorrelationId : IMutateIncomingTransportMessages, INeedInitialization
            {
                public Context Context { get; set; }

                public void MutateIncoming(TransportMessage transportMessage)
                {
                    Context.CorrelationIdReceived = transportMessage.CorrelationId;
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<GetValueOfIncomingCorrelationId>(DependencyLifecycle.InstancePerCall));
                }
            }

            public class SendMessageWithCorrelationHandler : IHandleMessages<SendMessageWithCorrelation>
            {
                public IBus Bus { get; set; }
                public Configure Configure { get; set; }

                public void Handle(SendMessageWithCorrelation message)
                {
                    Bus.Send(Configure.LocalAddress, CorrelationId, new MyRequest());
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
        public class SendMessageWithCorrelation : IMessage
        {
        }

        [Serializable]
        public class MyRequest : IMessage
        {
        }
    }
}
