namespace NServiceBus.AcceptanceTests.Hosting
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
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
                    c.EnableFeature<TimeoutManager>();
                });
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                Context.InputQueue = context.MessageHeaders[Headers.ReplyToAddress];
                Context.Done = true;
                return Task.FromResult(0);
            }
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