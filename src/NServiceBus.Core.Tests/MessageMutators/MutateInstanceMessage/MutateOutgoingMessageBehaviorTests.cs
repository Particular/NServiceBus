namespace NServiceBus.Core.Tests.MessageMutators.MutateInstanceMessage;

using System;
using System.Threading.Tasks;
using MessageMutator;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
class MutateOutgoingMessageBehaviorTests
{
    [Test]
    public async Task Should_invoke_all_explicit_mutators()
    {
        var mutator = new MutatorThatIndicatesIfItWasCalled();
        var otherMutator = new MutatorThatIndicatesIfItWasCalled();

        var behavior = new MutateOutgoingMessageBehavior([mutator, otherMutator]);

        var context = new TestableOutgoingLogicalMessageContext();

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mutator.MutateOutgoingCalled, Is.True);
            Assert.That(otherMutator.MutateOutgoingCalled, Is.True);
        }
    }

    [Test]
    public async Task Should_invoke_both_explicit_and_container_provided_mutators()
    {
        var explicitMutator = new MutatorThatIndicatesIfItWasCalled();
        var containerMutator = new MutatorThatIndicatesIfItWasCalled();

        var behavior = new MutateOutgoingMessageBehavior([explicitMutator]);

        var context = new TestableOutgoingLogicalMessageContext();
        context.Services.AddTransient<IMutateOutgoingMessages>(sp => containerMutator);

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(explicitMutator.MutateOutgoingCalled, Is.True);
            Assert.That(containerMutator.MutateOutgoingCalled, Is.True);
        }
    }

    [Test]
    public async Task Should_not_call_MutateOutgoing_when_hasOutgoingMessageMutators_is_false()
    {
        var behavior = new MutateOutgoingMessageBehavior([]);

        var context = new TestableOutgoingLogicalMessageContext();

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        var mutator = new MutatorThatIndicatesIfItWasCalled();
        context.Services.AddTransient<IMutateOutgoingMessages>(sp => mutator);

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(mutator.MutateOutgoingCalled, Is.False);
    }

    [Test]
    public void Should_throw_friendly_exception_when_IMutateOutgoingMessages_MutateOutgoing_returns_null()
    {
        var behavior = new MutateOutgoingMessageBehavior([]);

        var context = new TestableOutgoingLogicalMessageContext();
        context.Extensions.Set(new IncomingMessage("messageId", [], System.Array.Empty<byte>()));
        context.Extensions.Set(new LogicalMessage(null, null));
        context.Services.AddTransient<IMutateOutgoingMessages>(sp => new MutateOutgoingMessagesReturnsNull());

        Assert.That(async () => await behavior.Invoke(context, ctx => Task.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
    }

    [Test]
    public async Task When_no_mutator_updates_the_body_should_not_update_the_body()
    {
        var behavior = new MutateOutgoingMessageBehavior([]);

        var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

        context.Services.AddTransient<IMutateOutgoingMessages>(sp => new MutatorWhichDoesNotMutateTheBody());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageCalled, Is.False);
    }

    [Test]
    public async Task When_no_mutator_available_should_not_update_the_body()
    {
        var behavior = new MutateOutgoingMessageBehavior([]);

        var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

        context.Services.AddTransient(sp => System.Array.Empty<IMutateOutgoingMessages>());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageCalled, Is.False);
    }

    [Test]
    public async Task When_mutator_modifies_the_body_should_update_the_body()
    {
        var behavior = new MutateOutgoingMessageBehavior([]);

        var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

        context.Services.AddTransient<IMutateOutgoingMessages>(sp => new MutatorWhichMutatesTheBody());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageCalled, Is.True);
    }

    [Test]
    public async Task When_mutator_declares_a_message_type_should_use_the_explicit_type_overload()
    {
        var behavior = new MutateOutgoingMessageBehavior([]);

        var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

        context.Services.AddTransient<IMutateOutgoingMessages>(sp => new MutatorWhichDeclaresAMessageType());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.UpdateMessageObjCalled, Is.False);
            Assert.That(context.UpdateMessageWithTypeCalled, Is.True);
            Assert.That(context.DeclaredMessageType, Is.EqualTo(typeof(IMyMessage)));
            Assert.That(context.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }
    }

    [Test]
    public async Task When_mutator_uses_the_object_setter_should_use_the_object_overload()
    {
        var behavior = new MutateOutgoingMessageBehavior([]);

        var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

        context.Services.AddTransient<IMutateOutgoingMessages>(sp => new MutatorWhichMutatesTheBody());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.UpdateMessageObjCalled, Is.True);
            Assert.That(context.UpdateMessageWithTypeCalled, Is.False);
        }
    }

    class InterceptUpdateMessageOutgoingLogicalMessageContext : TestableOutgoingLogicalMessageContext
    {
        public bool UpdateMessageCalled { get; private set; }

        public bool UpdateMessageObjCalled { get; private set; }

        public bool UpdateMessageWithTypeCalled { get; private set; }

        public Type DeclaredMessageType { get; private set; }

        public override void UpdateMessage(object newInstance)
        {
            base.UpdateMessage(newInstance);

            UpdateMessageCalled = true;
            UpdateMessageObjCalled = true;
        }

        public override void UpdateMessage(object newInstance, Type messageType)
        {
            base.UpdateMessage(newInstance, messageType);

            UpdateMessageWithTypeCalled = true;
            DeclaredMessageType = messageType;
        }
    }

    class MutatorThatIndicatesIfItWasCalled : IMutateOutgoingMessages
    {
        public bool MutateOutgoingCalled { get; set; }

        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
            MutateOutgoingCalled = true;

            return Task.CompletedTask;
        }
    }

    class MutateOutgoingMessagesReturnsNull : IMutateOutgoingMessages
    {
        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
            return null;
        }
    }

    class MutatorWhichDoesNotMutateTheBody : IMutateOutgoingMessages
    {
        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
            return Task.CompletedTask;
        }
    }

    class MutatorWhichMutatesTheBody : IMutateOutgoingMessages
    {
        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
#pragma warning disable CS0618 // Deliberate coverage of the legacy runtime-type-routing setter until its removal
            context.OutgoingMessage = new object();
#pragma warning restore CS0618

            return Task.CompletedTask;
        }
    }

    class MutatorWhichDeclaresAMessageType : IMutateOutgoingMessages
    {
        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
            context.UpdateMessage<IMyMessage>(new MyMessage());

            return Task.CompletedTask;
        }
    }

    interface IMyMessage : IMessage
    { }

    class MyMessage : IMyMessage
    { }
}