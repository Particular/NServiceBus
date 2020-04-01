namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_aborting_the_behavior_chain : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Subsequent_handlers_will_not_be_invoked()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(b => b.When(session => session.SendLocal(new SomeMessage())))
                .Done(c => c.FirstHandlerInvoked)
                .Run();

            Assert.That(context.FirstHandlerInvoked, Is.True);
            Assert.That(context.SecondHandlerInvoked, Is.False);
        }

        public class Context : ScenarioContext
        {
            public bool FirstHandlerInvoked { get; set; }
            public bool SecondHandlerInvoked { get; set; }
        }

        public class SomeMessage : IMessage
        {
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.ExecuteTheseHandlersFirst(typeof(FirstHandler), typeof(SecondHandler)));
            }

            class FirstHandler : IHandleMessages<SomeMessage>
            {
                public FirstHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.FirstHandlerInvoked = true;

                    context.DoNotContinueDispatchingCurrentMessageToHandlers();

                    return Task.FromResult(0);
                }

                Context testContext;

            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public SecondHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.SecondHandlerInvoked = true;

                    return Task.FromResult(0);
                }

                Context testContext;

            }
        }
    }
}