namespace NServiceBus.AcceptanceTests.Callbacks
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_LegacyEnumResponse : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_send_back_old_style_control_message()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<EndpointWithLocalCallback>(b => b.Given(async (bus, c) =>
                    {
                        var response = bus.RequestResponseAsync<LegacyEnumResponse<OldEnum>>(new MyRequest(), new SynchronousOptions());

                        c.Response = await response;
                        c.CallbackFired = true;
                    }))
                .WithEndpoint<Replier>()
                .Done(c => c.CallbackFired)
                .Run();

            Assert.IsNotNull(context.Response);
            Assert.AreEqual(OldEnum.Success, context.Response.Status);
        }

        public class Context : ScenarioContext
        {
            public bool CallbackFired { get; set; }
            public LegacyEnumResponse<OldEnum> Response { get; set; }
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
                    Bus.Reply(new LegacyEnumResponse<OldEnum>(OldEnum.Success));
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

        public enum OldEnum
        {
            Fail,
            Success,
        }
    }
}