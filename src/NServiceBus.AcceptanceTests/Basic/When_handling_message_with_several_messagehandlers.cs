namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_handling_message_with_several_messagehandlers : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_call_all_handlers()
        {
            var context = new Context { Id = Guid.NewGuid() };

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage
                    {
                        Id = context.Id
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
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(Endpoint));
            }
        }

        [Serializable]
        public class MyMessage: IMessage
        {
            public Guid Id { get; set; }
        }

        public class FirstMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.FirstHandlerWasCalled = true;
            }
        }

        public class SecondMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public void Handle(MyMessage message)
            {
                if (Context.Id != message.Id)
                    return;

                Context.SecondHandlerWasCalled = true;
            }
        }


    }

}