namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_replying_with_simple_types : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw_a_good_exception()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(c => c.When(b => b.SendLocal(new MyRequest())))
                .Done(c => c.GotTheRequest)
                .Run();

            Assert.NotNull(context.ExceptionFromReply);
            StringAssert.Contains("Could not find metadata for", context.ExceptionFromReply.Message, "");
        }

        public class Context : ScenarioContext
        {
            public bool GotTheRequest { get; set; }
            public Exception ExceptionFromReply { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class StartMessageHandler : IHandleMessages<MyRequest>
            {
                public Context TestContext { get; set; }

                public async Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    try
                    {
                        await context.Reply(10);
                    }
                    catch (Exception ex)
                    {
                        TestContext.ExceptionFromReply = ex;
                    }

                    TestContext.GotTheRequest = true;
                }
            }
        }

        public class MyRequest : IMessage
        {
        }
    }
}
