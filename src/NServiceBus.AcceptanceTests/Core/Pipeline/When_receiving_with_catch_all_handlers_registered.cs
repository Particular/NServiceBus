namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_with_catch_all_handlers_registered : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_call_catch_all_handlers()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.SendLocal(new MyMessage
                {
                    Id = c.Id
                })))
                .Done(c => c.ObjectHandlerWasCalled && c.DynamicHandlerWasCalled && c.IMessageHandlerWasCalled)
                .Run();

            Assert.True(context.ObjectHandlerWasCalled);
            Assert.True(context.DynamicHandlerWasCalled);
            Assert.True(context.IMessageHandlerWasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool ObjectHandlerWasCalled { get; set; }
            public bool DynamicHandlerWasCalled { get; set; }
            public bool IMessageHandlerWasCalled { get; set; }
            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        public class CatchAllHandler_object : IHandleMessages<object>
        {
            public CatchAllHandler_object(Context context)
            {
                testContext = context;
            }

            public Task Handle(object message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                var myMessage = (MyMessage) message;
                if (testContext.Id != myMessage.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.ObjectHandlerWasCalled = true;
                return Task.FromResult(0);
            }

            Context testContext;
        }

        public class CatchAllHandler_dynamic : IHandleMessages<object>
        {
            public CatchAllHandler_dynamic(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(dynamic message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                var myMessage = (MyMessage) message;
                if (testContext.Id != myMessage.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.DynamicHandlerWasCalled = true;

                return Task.FromResult(0);
            }

            Context testContext;
        }

        public class CatchAllHandler_IMessage : IHandleMessages<IMessage>
        {
            public CatchAllHandler_IMessage(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(IMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                var myMessage = (MyMessage) message;
                if (testContext.Id != myMessage.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.IMessageHandlerWasCalled = true;
                return Task.FromResult(0);
            }

            Context testContext;
        }
    }
}