﻿namespace NServiceBus.Core.Tests.MessageMutators.MutateTransportMessage;

using System;
using System.Threading.Tasks;
using MessageMutator;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Testing;

[TestFixture]
class MutateOutgoingTransportMessageBehaviorTests
{
    [Test]
    public async Task Should_invoke_all_explicit_mutators()
    {
        var mutator = new MutatorThatIndicatesIfItWasCalled();
        var otherMutator = new MutatorThatIndicatesIfItWasCalled();

        var behavior = new MutateOutgoingTransportMessageBehavior([mutator, otherMutator]);

        var physicalContext = new TestableOutgoingPhysicalMessageContext();
        physicalContext.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

        await behavior.Invoke(physicalContext, ctx => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(mutator.MutateOutgoingCalled, Is.True);
            Assert.That(otherMutator.MutateOutgoingCalled, Is.True);
        });
    }

    [Test]
    public async Task Should_invoke_both_explicit_and_container_provided_mutators()
    {
        var explicitMutator = new MutatorThatIndicatesIfItWasCalled();
        var containerMutator = new MutatorThatIndicatesIfItWasCalled();

        var behavior = new MutateOutgoingTransportMessageBehavior([explicitMutator]);

        var physicalContext = new TestableOutgoingPhysicalMessageContext();
        physicalContext.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));
        physicalContext.Services.AddTransient<IMutateOutgoingTransportMessages>(sp => containerMutator);

        await behavior.Invoke(physicalContext, ctx => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(explicitMutator.MutateOutgoingCalled, Is.True);
            Assert.That(containerMutator.MutateOutgoingCalled, Is.True);
        });
    }

    [Test]
    public async Task Should_not_call_MutateOutgoing_when_hasOutgoingTransportMessageMutators_is_false()
    {
        var behavior = new MutateOutgoingTransportMessageBehavior([]);

        var physicalContext = new TestableOutgoingPhysicalMessageContext();
        physicalContext.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

        await behavior.Invoke(physicalContext, ctx => Task.CompletedTask);

        var mutator = new MutatorThatIndicatesIfItWasCalled();
        physicalContext.Services.AddTransient<IMutateOutgoingTransportMessages>(sp => mutator);

        await behavior.Invoke(physicalContext, ctx => Task.CompletedTask);

        Assert.That(mutator.MutateOutgoingCalled, Is.False);
    }

    [Test]
    public void Should_throw_friendly_exception_when_IMutateOutgoingTransportMessages_MutateOutgoing_returns_null()
    {
        var behavior = new MutateOutgoingTransportMessageBehavior([]);

        var physicalContext = new TestableOutgoingPhysicalMessageContext();
        physicalContext.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));
        physicalContext.Services.AddTransient<IMutateOutgoingTransportMessages>(sp => new MutateOutgoingTransportMessagesReturnsNull());

        Assert.That(async () => await behavior.Invoke(physicalContext, ctx => Task.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
    }

    [Test]
    public async Task When_no_mutator_updates_the_body_should_not_update_the_body()
    {
        var behavior = new MutateOutgoingTransportMessageBehavior([]);

        var context = new InterceptUpdateMessageOutgoingPhysicalMessageContext();
        context.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

        context.Services.AddTransient<IMutateOutgoingTransportMessages>(sp => new MutatorWhichDoesNotMutateTheBody());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageCalled, Is.False);
    }

    [Test]
    public async Task When_no_mutator_available_should_not_update_the_body()
    {
        var behavior = new MutateOutgoingTransportMessageBehavior([]);

        var context = new InterceptUpdateMessageOutgoingPhysicalMessageContext();
        context.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

        context.Services.AddTransient(sp => Array.Empty<IMutateOutgoingTransportMessages>());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageCalled, Is.False);
    }

    [Test]
    public async Task When_mutator_modifies_the_body_should_update_the_body()
    {
        var behavior = new MutateOutgoingTransportMessageBehavior([]);

        var context = new InterceptUpdateMessageOutgoingPhysicalMessageContext();
        context.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

        context.Services.AddTransient<IMutateOutgoingTransportMessages>(sp => new MutatorWhichMutatesTheBody());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageCalled, Is.True);
    }

    class InterceptUpdateMessageOutgoingPhysicalMessageContext : TestableOutgoingPhysicalMessageContext
    {
        public bool UpdateMessageCalled { get; private set; }

        public override void UpdateMessage(ReadOnlyMemory<byte> body)
        {
            base.UpdateMessage(body);

            UpdateMessageCalled = true;
        }
    }

    class MutatorThatIndicatesIfItWasCalled : IMutateOutgoingTransportMessages
    {
        public bool MutateOutgoingCalled { get; set; }

        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            MutateOutgoingCalled = true;

            return Task.CompletedTask;
        }
    }

    class MutateOutgoingMessagesReturnsNull : IMutateOutgoingTransportMessages
    {
        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            return null;
        }
    }

    class MutatorWhichDoesNotMutateTheBody : IMutateOutgoingTransportMessages
    {
        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            return Task.CompletedTask;
        }
    }

    class MutatorWhichMutatesTheBody : IMutateOutgoingTransportMessages
    {
        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            context.OutgoingBody = Array.Empty<byte>();

            return Task.CompletedTask;
        }
    }

    class MutateOutgoingTransportMessagesReturnsNull : IMutateOutgoingTransportMessages
    {
        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            return null;
        }
    }

    class FakeMessage : IMessage
    {
    }
}