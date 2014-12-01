namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_registering_a_callback_for_a_local_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_trigger_the_callback_when_the_response_comes_back()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithLocalCallback>(b=>b.Given(
                        (bus,context)=>bus.SendLocal(new MyRequest()).Register(r => context.CallbackFired = DateTime.UtcNow)))
                    .Done(c => c.HandlerGotTheRequest.HasValue)
                    .Repeat(r =>r.For(Transports.Default))
                    .Should(c =>
                        {
                            Assert.Greater(c.CallbackFired,c.HandlerGotTheRequest,"The callback should fire when the response comes in");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public DateTime? HandlerGotTheRequest { get; set; }


            public DateTime CallbackFired { get; set; }
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
                    Context.HandlerGotTheRequest = DateTime.UtcNow;

                    Bus.Return(1);
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage{}
    }
}
