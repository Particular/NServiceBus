namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_a_struct : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task When_json_hould_receive_the_message()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var message = new MyMessage
                    {
                        Id = c.Id
                    };
                    return session.SendLocal(message);
                }))
                .Done(c => c.WasCalled)
                .Run(TimeSpan.FromSeconds(10));

            Assert.True(context.WasCalled, "The message handler should be called");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseSerialization<JsonSerializer>();
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    if (TestContext.Id == message.Id)
                    {
                        TestContext.WasCalled = true;
                    }
                    return Task.FromResult(0);
                }
            }
        }

        public struct MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}