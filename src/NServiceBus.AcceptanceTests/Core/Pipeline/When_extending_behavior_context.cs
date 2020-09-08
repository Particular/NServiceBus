namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_extending_behavior_context : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_available_in_handler_context()
        {
            ExtensionValue = Guid.NewGuid().ToString();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ContextExtendingEndpoint>(e => e
                    .When((session, c) => session.SendLocal(new SomeMessage())))
                .Done(c => c.HandlerAExtensionValue != null && c.HandlerBExtensionValue != null)
                .Run();

            Assert.AreEqual(ExtensionValue, context.HandlerAExtensionValue);
            Assert.AreEqual(ExtensionValue, context.HandlerBExtensionValue);
        }

        static string ExtensionValue;

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
                    new CustomContextExtensionBehavior(),
                    "Puts customized data on the message context"));
            }

            class MessageHandlerA : IHandleMessages<SomeMessage>
            {
                public MessageHandlerA(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    context.Extensions.TryGet("CustomExtension", out string extensionValue);
                    testContext.HandlerAExtensionValue = extensionValue;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class MessageHandlerB : IHandleMessages<SomeMessage>
            {
                public MessageHandlerB(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    context.Extensions.TryGet("CustomExtension", out string extensionValue);
                    testContext.HandlerBExtensionValue = extensionValue;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class CustomContextExtensionBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
            {
                public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
                {
                    context.Extensions.Set("CustomExtension", ExtensionValue);
                    return next(context, cancellationToken);
                }
            }
        }

        public class SomeMessage : ICommand
        {
        }
    }
}