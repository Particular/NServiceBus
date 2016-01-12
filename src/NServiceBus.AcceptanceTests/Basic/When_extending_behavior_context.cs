namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_extending_behavior_context : NServiceBusAcceptanceTest
    {
        static string ExtensionValue;

        [Test]
        public async Task Should_be_available_in_handler_context()
        {
            ExtensionValue = Guid.NewGuid().ToString();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ContextExtendingEndpoint>(e => e
                    .When((bus, c) => bus.SendLocal(new SomeMessage())))
                .Done(c => c.HandlerAExtensionValue != null && c.HandlerBExtensionValue != null)
                .Run();

            Assert.AreEqual(ExtensionValue, context.HandlerAExtensionValue);
            Assert.AreEqual(ExtensionValue, context.HandlerBExtensionValue);
        }

        class Context : ScenarioContext
        {
            public string HandlerAExtensionValue { get; set; }
            public string HandlerBExtensionValue { get; set; }
        }

        class ContextExtendingEndpoint : EndpointConfigurationBuilder
        {
            public ContextExtendingEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register(
                    "CustomContextExtensionBehavior",
                    typeof(CustomContextExtensionBehavior),
                    "Puts customized data on the message context"));
            }

            class MessageHandlerA : IHandleMessages<SomeMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    string extensionValue;
                    context.Extensions.TryGet("CustomExtension", out extensionValue);
                    TestContext.HandlerAExtensionValue = extensionValue;
                    return Task.FromResult(0);
                }
            }

            class MessageHandlerB : IHandleMessages<SomeMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    string extensionValue;
                    context.Extensions.TryGet("CustomExtension", out extensionValue);
                    TestContext.HandlerBExtensionValue = extensionValue;
                    return Task.FromResult(0);
                }
            }

            class CustomContextExtensionBehavior : Behavior<IIncomingLogicalMessageContext>
            {
                public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    context.Extensions.Set("CustomExtension", ExtensionValue);
                    return next();
                }
            }
        }

        class SomeMessage : ICommand
        {
        }
    }
}