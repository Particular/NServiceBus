namespace NServiceBus.AcceptanceTests.Core.Diagnostics
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_overriding_input_queue_name : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_custom_queue_names()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(e => e.When(b => b.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run();

            Assert.IsTrue(context.Done);
            Assert.IsTrue(context.InputQueue.StartsWith("OverriddenInputQueue"));
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>((c, d) =>
                {
                    c.OverrideLocalAddress("OverriddenInputQueue");
                });
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.InputQueue = context.MessageHeaders[Headers.ReplyToAddress];
                testContext.Done = true;
                return Task.FromResult(0);
            }

            Context testContext;
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string InputQueue { get; set; }
        }

        public class MyMessage : ICommand
        {
        }
    }
}