﻿namespace NServiceBus.Core.Tests.MessageMutators.MutateTransportMessage;

using System;
using System.Threading.Tasks;
using MessageMutator;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;

[TestFixture]
public class MutateIncomingTransportMessageBehaviorTests
{
    [Test]
    public async Task Should_invoke_all_explicit_mutators()
    {
        var mutator = new MutatorThatIndicatesIfItWasCalled();
        var otherMutator = new MutatorThatIndicatesIfItWasCalled();

        var behavior = new MutateIncomingTransportMessageBehavior([mutator, otherMutator]);

        var context = new TestableIncomingPhysicalMessageContext();

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(mutator.MutateIncomingCalled, Is.True);
        Assert.That(otherMutator.MutateIncomingCalled, Is.True);
    }

    [Test]
    public async Task Should_invoke_both_explicit_and_container_provided_mutators()
    {
        var explicitMutator = new MutatorThatIndicatesIfItWasCalled();
        var containerMutator = new MutatorThatIndicatesIfItWasCalled();

        var behavior = new MutateIncomingTransportMessageBehavior([explicitMutator]);

        var context = new TestableIncomingPhysicalMessageContext();
        context.Services.AddTransient<IMutateIncomingTransportMessages>(sp => containerMutator);

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(explicitMutator.MutateIncomingCalled, Is.True);
        Assert.That(containerMutator.MutateIncomingCalled, Is.True);
    }

    [Test]
    public async Task Should_not_call_MutateIncoming_when_hasIncomingTransportMessageMutators_is_false()
    {
        var behavior = new MutateIncomingTransportMessageBehavior([]);

        var context = new TestableIncomingPhysicalMessageContext();

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        var mutator = new MutatorThatIndicatesIfItWasCalled();
        context.Services.AddTransient<IMutateIncomingTransportMessages>(sp => mutator);

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(mutator.MutateIncomingCalled, Is.False);
    }

    [Test]
    public void Should_throw_friendly_exception_when_IMutateIncomingTransportMessages_MutateIncoming_returns_null()
    {
        var behavior = new MutateIncomingTransportMessageBehavior([]);

        var context = new TestableIncomingPhysicalMessageContext();

        context.Services.AddTransient<IMutateIncomingTransportMessages>(sp => new MutateIncomingTransportMessagesReturnsNull());

        Assert.That(async () => await behavior.Invoke(context, ctx => Task.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
    }

    [Test]
    public async Task When_no_mutator_updates_the_body_should_not_update_the_body()
    {
        var behavior = new MutateIncomingTransportMessageBehavior([]);

        var context = new InterceptUpdateMessageIncomingPhysicalMessageContext();

        context.Services.AddTransient<IMutateIncomingTransportMessages>(sp => new MutatorWhichDoesNotMutateTheBody());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageBodyCalled, Is.False);
    }

    [Test]
    public async Task When_no_mutator_available_should_not_update_the_body()
    {
        var behavior = new MutateIncomingTransportMessageBehavior([]);

        var context = new InterceptUpdateMessageIncomingPhysicalMessageContext();

        context.Services.AddTransient(sp => Array.Empty<IMutateIncomingTransportMessages>());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageBodyCalled, Is.False);
    }

    [Test]
    public async Task When_mutator_modifies_the_body_should_update_the_body()
    {
        var behavior = new MutateIncomingTransportMessageBehavior([]);

        var context = new InterceptUpdateMessageIncomingPhysicalMessageContext();

        context.Services.AddTransient<IMutateIncomingTransportMessages>(sp => new MutatorWhichMutatesTheBody());

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.UpdateMessageBodyCalled, Is.True);
    }

    class InterceptUpdateMessageIncomingPhysicalMessageContext : TestableIncomingPhysicalMessageContext
    {
        public bool UpdateMessageBodyCalled { get; private set; }

        public override void UpdateMessage(ReadOnlyMemory<byte> body)
        {
            base.UpdateMessage(body);

            UpdateMessageBodyCalled = true;
        }
    }

    class MutatorThatIndicatesIfItWasCalled : IMutateIncomingTransportMessages
    {
        public bool MutateIncomingCalled { get; set; }

        public Task MutateIncoming(MutateIncomingTransportMessageContext context)
        {
            MutateIncomingCalled = true;

            return Task.CompletedTask;
        }
    }

    class MutateIncomingTransportMessagesReturnsNull : IMutateIncomingTransportMessages
    {
        public Task MutateIncoming(MutateIncomingTransportMessageContext context)
        {
            return null;
        }
    }

    class MutatorWhichDoesNotMutateTheBody : IMutateIncomingTransportMessages
    {
        public Task MutateIncoming(MutateIncomingTransportMessageContext context)
        {
            return Task.CompletedTask;
        }
    }

    class MutatorWhichMutatesTheBody : IMutateIncomingTransportMessages
    {
        public Task MutateIncoming(MutateIncomingTransportMessageContext context)
        {
            context.Body = Array.Empty<byte>();

            return Task.CompletedTask;
        }
    }
}
