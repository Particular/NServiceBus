namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_registering_a_callback_from_early_version_missing_intent : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_trigger_the_callback()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithLocalCallback>(b=>b.Given(
                        (bus,context)=>bus.SendLocal(new MyRequest()).Register(r =>
                        {
                            Assert.True(context.HandlerGotTheRequest);
                            context.CallbackFired = true;
                        })))
                    .Done(c => c.CallbackFired)
                    .Repeat(r =>r.For(Transports.Default))
                    .Should(c =>
                    {
                        Assert.True(c.CallbackFired);
                        Assert.True(c.HandlerGotTheRequest);
                    })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool HandlerGotTheRequest { get; set; }
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
                    Context.HandlerGotTheRequest = true;

                    Bus.Return(1);
                }
            }
        }

        class BodyMutator : IMutateIncomingTransportMessages, INeedInitialization
        {
            public Context Context { get; set; }

            public void MutateIncoming(TransportMessage transportMessage)
            {
                //early versions of did not have a Reply MessageIntent when Bus.Return is called 
                transportMessage.MessageIntent = MessageIntentEnum.Send;
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
