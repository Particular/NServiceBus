namespace NServiceBus.Core.Tests.MessageMutators.MutateInstanceMessage;

using System;
using System.Threading.Tasks;
using MessageMutator;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.MessageInterfaces;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Testing;
using Unicast.Messages;

[TestFixture]
public class When_incoming_mutator_replaces_message_instance
{
    [Test]
    public async Task Should_recompute_metadata_for_the_replacement_instance_type()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(OriginalMessage), typeof(ReplacementMessage)]);

        var context = CreateContext(registry, new ReplaceWithReplacementMessageMutator());

        await context.Behavior.Invoke(context.Context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Context.Message.Instance, Is.TypeOf<ReplacementMessage>());
            Assert.That(context.Context.Message.MessageType, Is.EqualTo(typeof(ReplacementMessage)));
            Assert.That(context.Context.Message.Metadata.MessageType, Is.EqualTo(typeof(ReplacementMessage)));
        }
    }

    [Test]
    public async Task Should_keep_original_metadata_when_instance_is_not_replaced()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(OriginalMessage), typeof(ReplacementMessage)]);

        var context = CreateContext(registry, new DoNothingMutator());

        await context.Behavior.Invoke(context.Context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Context.Message.Instance, Is.TypeOf<OriginalMessage>());
            Assert.That(context.Context.Message.MessageType, Is.EqualTo(typeof(OriginalMessage)));
        }
    }

    static ContextFixture CreateContext(MessageMetadataRegistry registry, IMutateIncomingMessages mutator)
    {
        var services = new ServiceCollection();
        services.AddSingleton(registry);
        services.AddSingleton<LogicalMessageFactory>();
        services.AddSingleton<IMessageMapper>(new TrimmingSafeMessageMapper());
        IServiceProvider provider = services.BuildServiceProvider();

        var parentContext = new TestableIncomingPhysicalMessageContext();
        parentContext.Extensions.Set(provider);

        var logicalMessage = new LogicalMessage(registry.GetMessageMetadata(typeof(OriginalMessage)), new OriginalMessage());
        var context = new IncomingLogicalMessageContext(logicalMessage, parentContext);
        var behavior = new MutateIncomingMessageBehavior([mutator]);

        return new ContextFixture(context, behavior);
    }

    class ContextFixture(IncomingLogicalMessageContext context, MutateIncomingMessageBehavior behavior)
    {
        public IncomingLogicalMessageContext Context { get; } = context;
        public MutateIncomingMessageBehavior Behavior { get; } = behavior;
    }

    class ReplaceWithReplacementMessageMutator : IMutateIncomingMessages
    {
        public Task MutateIncoming(MutateIncomingMessageContext context)
        {
            context.Message = new ReplacementMessage();
            return Task.CompletedTask;
        }
    }

    class DoNothingMutator : IMutateIncomingMessages
    {
        public Task MutateIncoming(MutateIncomingMessageContext context) => Task.CompletedTask;
    }

    public class OriginalMessage : IMessage
    {
        public string SomeProperty { get; set; }
    }

    public class ReplacementMessage : IMessage
    {
        public string SomeProperty { get; set; }
    }
}
