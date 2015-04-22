namespace NServiceBus.AcceptanceTests.Callbacks
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_callback_vnext_to_get_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_message()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<EndpointWithLocalCallback>(b => b.Given(async (bus, c) =>
                    {
                        var response = bus.SynchronousRequestResponse<MyResponse>(new MyRequest(), new SynchronousOptions());

                        c.Response = await response.ResponseTask;
                        c.CallbackFired = true;
                    }))
                .WithEndpoint<Replier>()
                .Done(c => c.CallbackFired)
                .Run();

            Assert.IsNotNull(context.Response);
        }

        public class Context : ScenarioContext
        {
            public bool CallbackFired { get; set; }
            public MyResponse Response { get; set; }
        }

        public class Replier : EndpointConfigurationBuilder
        {
            public Replier()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public IBus Bus { get; set; }

                public void Handle(MyRequest request)
                {
                    Bus.Reply(new MyResponse());
                }
            }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyRequest>(typeof(Replier));
            }
        }

        [Serializable]
        public class MyRequest : IMessage { }

        [Serializable]
        public class MyResponse : IMessage { }
    }
}