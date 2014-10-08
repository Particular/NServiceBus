namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_callback_to_get_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_message()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<EndpointWithLocalCallback>(b => b.Given(
                    (bus, c) => bus.SendLocal(new MyRequest()).Register(r =>
                    {
                        c.Reply = (MyReply)r.Messages.Single();
                        c.CallbackFired = true;
                    })))
                .Done(c => c.CallbackFired)
                .Run();

            Assert.IsNotNull(context.Reply);
        }

        public class Context : ScenarioContext
        {
            public bool CallbackFired { get; set; }
            public MyReply Reply { get; set; }
        }

        public class EndpointWithLocalCallback : EndpointConfigurationBuilder
        {
            public EndpointWithLocalCallback()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public IBus Bus { get; set; }

                public void Handle(MyRequest request)
                {
                    Bus.Reply(new MyReply());
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage { }

        [Serializable]
        public class MyReply : IMessage { }
    }
}