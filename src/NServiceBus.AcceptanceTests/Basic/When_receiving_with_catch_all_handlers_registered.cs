namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_with_catch_all_handlers_registered : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_call_catch_all_handlers()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) => bus.SendLocal(new MyMessage
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

        [Serializable]
        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        public class CatchAllHandler_object : IHandleMessages<object>
        {
            public Context Context { get; set; }

            public Task Handle(object message, IMessageHandlerContext context)
            {
                var myMessage = (MyMessage)message;
                if (Context.Id != myMessage.Id)
                    return Task.FromResult(0);

                Context.ObjectHandlerWasCalled = true;
                return Task.FromResult(0);
            }
        }

        public class CatchAllHandler_dynamic : IHandleMessages<object>
        {
            public Context Context { get; set; }

            public Task Handle(dynamic message, IMessageHandlerContext context)
            {
                var myMessage = (MyMessage)message;
                if (Context.Id != myMessage.Id)
                    return Task.FromResult(0);

                Context.DynamicHandlerWasCalled = true;

                return Task.FromResult(0);
            }
        }

        public class CatchAllHandler_IMessage : IHandleMessages<IMessage>
        {
            public Context Context { get; set; }

            public Task Handle(IMessage message, IMessageHandlerContext context)
            {
                var myMessage = (MyMessage)message;
                if (Context.Id != myMessage.Id)
                    return Task.FromResult(0);

                Context.IMessageHandlerWasCalled = true;
                return Task.FromResult(0);
            }
        }
    }

}