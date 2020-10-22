namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_handling_message_with_several_messagehandlers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_call_all_handlers()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage
                {
                    Id = c.Id
                })))
                .Done(c => c.FirstHandlerWasCalled)
                .Run();

            Assert.True(context.FirstHandlerWasCalled);
            Assert.True(context.SecondHandlerWasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool FirstHandlerWasCalled { get; set; }
            public bool SecondHandlerWasCalled { get; set; }
            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Routing().RouteToEndpoint(typeof(MyMessage), typeof(Endpoint)));
            }
        }

        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        public class FirstMessageHandler : IHandleMessages<MyMessage>
        {
            public FirstMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (testContext.Id != message.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.FirstHandlerWasCalled = true;

                return Task.FromResult(0);
            }

            Context testContext;
        }

        public class SecondMessageHandler : IHandleMessages<MyMessage>
        {
            public SecondMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (testContext.Id != message.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.SecondHandlerWasCalled = true;

                return Task.FromResult(0);
            }

            Context testContext;
        }
    }
}