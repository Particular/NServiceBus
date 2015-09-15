namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_overriding_public_return_address : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task A_reply_should_be_delivered_to_the_overridden_address()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.Given(b => b.SendLocalAsync(new MyMessage())))
                .WithEndpoint<Detector>()
                .Done(c => c.GotReply)
                .Run();

            Assert.IsTrue(ctx.GotReply);
        }

        public class Context : ScenarioContext
        {
            public bool GotReply { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultPublisher>(b => b.OverridePublicReturnAddress("overridingpublicreturnaddress.detector"));
            }

            public class MessageHandler : IHandleMessages<MyMessage>
            {
                public IBus Bus { get; set; }

                public Task Handle(MyMessage messageThatIsEnlisted)
                {
                    return Bus.ReplyAsync(new MyReply());
                }
            }
        }

        public class Detector : EndpointConfigurationBuilder
        {
            public Detector()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                });
            }

            public class ReplyHandler : IHandleMessages<MyReply>
            {
                public Context Context { get; set; }

                public Task Handle(MyReply messageThatIsEnlisted)
                {
                    Context.GotReply = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
        
        public class MyReply : IMessage
        {
        }
    }
}
