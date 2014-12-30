namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;

    public class When_using_callbacks_with_messageid_eq_cid_ : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_trigger_the_callback()
        {
            var context = Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithLocalCallback>(b=>b.Given(
                        (bus,c)=>bus.SendLocal(new MyRequest()).Register(r =>
                        {
                            c.CallbackFired = true;
                        })))
                    .Done(c => c.CallbackFired)
                    .Run();

            Assert.True(context.CallbackFired);
        }

        public class Context : ScenarioContext
        {
            public bool CallbackFired { get; set; }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest request)
                {
                    Assert.False(Context.CallbackFired);

                    Bus.Return(1);
                }
            }
        }

        class BodyMutator : IMutateOutgoingTransportMessages, INeedInitialization
        {
            public void MutateOutgoing(LogicalMessage logicalMessage, TransportMessage transportMessage)
            {
                //to simulate native interop cases where MessageId == CorrelationId
                transportMessage.Headers[Headers.MessageId] = transportMessage.Headers[Headers.CorrelationId];
            }

            public void Customize(BusConfiguration configuration)
            {
                configuration.RegisterComponents(c => c.ConfigureComponent<BodyMutator>(DependencyLifecycle.InstancePerCall));
            }

         
        }

        [Serializable]
        public class MyRequest : IMessage{}
    }
}
