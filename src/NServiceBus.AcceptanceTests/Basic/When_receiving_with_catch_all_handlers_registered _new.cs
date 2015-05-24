namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_with_catch_all_handlers_registered_new : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_call_catch_all_handlers()
        {
            var context = new Context { Id = Guid.NewGuid() };

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage
                    {
                        Id = context.Id
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
        public class MyMessage: ICommand
        {
            public Guid Id { get; set; }
        }

        // TODO: From a catch all perspective IHandleMessages kinda makes sense. Thoughts?
        public class CatchAllHandler_object : IProcessCommands<object>
        {
            public Context Context { get; set; }

            public void Handle(object message, ICommandContext context)
            {
                var myMessage = (MyMessage)message;
                if (Context.Id != myMessage.Id)
                    return;

                Context.ObjectHandlerWasCalled = true;
            }
        }

        // TODO: From a catch all perspective IHandleMessages kinda makes sense. Thoughts?
        public class CatchAllHandler_dynamic : IProcessCommands<object>
        {
            public Context Context { get; set; }

            public void Handle(dynamic message, ICommandContext context)
            {
                var myMessage = (MyMessage)message;
                if (Context.Id != myMessage.Id)
                    return;

                Context.DynamicHandlerWasCalled = true;
            }
        }

        public class CatchAllHandler_IMessage : IProcessCommands<ICommand>
        {
            public Context Context { get; set; }
            
            public void Handle(ICommand message, ICommandContext context)
            {
                var myMessage = (MyMessage) message;
                if (Context.Id != myMessage.Id)
                    return;

                Context.IMessageHandlerWasCalled = true;
            }
        }
    }

}