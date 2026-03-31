namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HandlerAExtensionValue, Is.EqualTo(ExtensionValue));
            Assert.That(context.HandlerBExtensionValue, Is.EqualTo(ExtensionValue));
        }
    }

    static string ExtensionValue;

    public class Context : ScenarioContext
    {
        public string HandlerAExtensionValue { get; set; }
        public string HandlerBExtensionValue { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(HandlerAExtensionValue != null, HandlerBExtensionValue != null);
    }

    public class ContextExtendingEndpoint : EndpointConfigurationBuilder
    {
        public ContextExtendingEndpoint() =>
            EndpointSetup<DefaultServer>(c => c.Pipeline.Register(
                "CustomContextExtensionBehavior",
                new CustomContextExtensionBehavior(),
                "Puts customized data on the message context"));

        [Handler]
        public class MessageHandlerA(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                context.Extensions.TryGet("CustomExtension", out string extensionValue);
                testContext.HandlerAExtensionValue = extensionValue;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class MessageHandlerB(Context testContext) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                context.Extensions.TryGet("CustomExtension", out string extensionValue);
                testContext.HandlerBExtensionValue = extensionValue;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }

        class CustomContextExtensionBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
        {
            public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
            {
                context.Extensions.Set("CustomExtension", ExtensionValue);
                return next(context);
            }
        }
    }

    public class SomeMessage : ICommand;
}