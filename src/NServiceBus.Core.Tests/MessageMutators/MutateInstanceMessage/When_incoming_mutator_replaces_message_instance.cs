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
        var context = CreateContext(new ReplaceWithReplacementMessageMutator());
        var behavior = new MutateIncomingMessageBehavior([]);

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Message.Instance, Is.TypeOf<ReplacementMessage>());
            Assert.That(context.Message.MessageType, Is.EqualTo(typeof(ReplacementMessage)));
            Assert.That(context.Message.Metadata.MessageType, Is.EqualTo(typeof(ReplacementMessage)));
        }
    }

    [Test]
    public async Task Should_keep_original_metadata_when_instance_is_not_replaced()
    {
        var context = CreateContext(new DoNothingMutator());
        var behavior = new MutateIncomingMessageBehavior([]);

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Message.Instance, Is.TypeOf<OriginalMessage>());
            Assert.That(context.Message.MessageType, Is.EqualTo(typeof(OriginalMessage)));
        }
    }

    static IncomingLogicalMessageContext CreateContext(IMutateIncomingMessages mutator)
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(OriginalMessage), typeof(ReplacementMessage)]);

        var services = new ServiceCollection();
        services.AddSingleton(registry);
        services.AddSingleton<LogicalMessageFactory>();
        services.AddSingleton<IMessageMapper>(new TrimmingSafeMessageMapper());
        services.AddSingleton(mutator);
        IServiceProvider provider = services.BuildServiceProvider();

        var parentContext = new TestableIncomingPhysicalMessageContext();
        parentContext.Extensions.Set(provider);

        var logicalMessage = new LogicalMessage(registry.GetMessageMetadata(typeof(OriginalMessage)), new OriginalMessage());

        return new IncomingLogicalMessageContext(logicalMessage, parentContext);
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
