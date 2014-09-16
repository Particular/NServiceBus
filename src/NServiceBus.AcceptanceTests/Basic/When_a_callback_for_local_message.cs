namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_a_callback_for_local_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_trigger_the_callback_when_the_response_comes_back()
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

        [Serializable]
        public class MyRequest : IMessage{}
    }
}
