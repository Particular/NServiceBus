namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Pipeline;
    using NUnit.Framework;

    public class When_extending_sendoptions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_set_context_items_and_retrieve_it_via_a_behavior()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<SendOptionsExtensions>(b => b.When((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.GetExtensions().Set(new SendOptionsExtensions.TestingSendOptionsExtensionBehavior.Context { SomeValue = "I did it" });
                        options.RouteToLocalEndpointInstance();

                        return bus.Send(new SendMessage(), options);
                    }))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.AreEqual("I did it", context.Secret);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public string Secret { get; set; }
        }

        public class SendOptionsExtensions : EndpointConfigurationBuilder
        {
            public SendOptionsExtensions()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("TestingSendOptionsExtension", typeof(TestingSendOptionsExtensionBehavior), "Testing send options extensions"));
            }

            class SendMessageHandler : IHandleMessages<SendMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(SendMessage message, IMessageHandlerContext context)
                {
                    TestContext.Secret = message.Secret;
                    TestContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }

            public class TestingSendOptionsExtensionBehavior : Behavior<IOutgoingLogicalMessageContext>
            {
                public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                {
                    Context data;
                    if (context.Extensions.TryGet(out data))
                    {
                        context.UpdateMessageInstance(new SendMessage { Secret = data.SomeValue });
                    }

                    return next();
                }

                public class Context
                {
                    public string SomeValue { get; set; }
                }
            }
        }

        [Serializable]
        public class SendMessage : ICommand
        {
            public string Secret { get; set; }
        }
    }
}
