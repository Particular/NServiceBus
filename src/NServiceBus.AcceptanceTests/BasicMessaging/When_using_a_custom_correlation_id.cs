namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using MessageMutator;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_a_custom_correlation_id : NServiceBusAcceptanceTest
    {
        static string CorrelationId = "my_custom_correlation_id";
       
        [Test]
        public void Should_use_the_given_id_as_the_transport_level_correlation_id()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<CorrelationEndpoint>(b => b.Given(bus => bus.Send(Address.Local, CorrelationId, new MyRequest())))
                    .Done(c => c.GotRequest)
                    .Repeat(r => r.For<AllTransports>()
                    )
                    .Should(c =>
                        {
                            Assert.AreEqual(CorrelationId, c.CorrelationIdReceived,"Correlation ids should match");
                        })
                    .Run();
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

            class GetValueOfIncomingCorrelationId:IMutateIncomingTransportMessages,INeedInitialization
            {
                public Context Context { get; set; }

                public void MutateIncoming(TransportMessage transportMessage)
                {
                    Context.CorrelationIdReceived = transportMessage.CorrelationId;
                }

                public void Init()
                {
                    Configure.Component<GetValueOfIncomingCorrelationId>(DependencyLifecycle.InstancePerCall);
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
