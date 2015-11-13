namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_aborting_the_behavior_chain : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Subsequent_handlers_will_not_be_invoked()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(b => b.When(bus => bus.SendLocal(new SomeMessage())))
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

        [Serializable]
        public class SomeMessage : IMessage { }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.ExecuteTheseHandlersFirst(typeof(FirstHandler), typeof(SecondHandler)));
            }

            class FirstHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    Context.FirstHandlerInvoked = true;

                    context.DoNotContinueDispatchingCurrentMessageToHandlers();

                    return Task.FromResult(0);
                }
            }

            class SecondHandler : IHandleMessages<SomeMessage>
            {
                public Context Context { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    Context.SecondHandlerInvoked = true;

                    return Task.FromResult(0);
                }
            }
        }
    }
}