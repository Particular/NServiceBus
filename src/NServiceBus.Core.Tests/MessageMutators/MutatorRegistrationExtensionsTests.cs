﻿namespace NServiceBus.Core.Tests.MessageMutators;

using System;
using System.Threading.Tasks;
using MessageMutator;
using NUnit.Framework;

[TestFixture]
public class MutatorRegistrationExtensionsTests
{
    [Test]
    public void Should_throw_ArgumentException_when_registering_non_mutator_type()
    {
        var endpointConfiguration = new EndpointConfiguration("test");

        var exception = Assert.Throws<ArgumentException>(() => endpointConfiguration.RegisterMessageMutator(new object()));
        Assert.That(
            exception.Message,
            Does.Contain("The given instance is not a valid message mutator. Implement one of the following mutator interfaces: NServiceBus.MessageMutator.IMutateIncomingMessages, NServiceBus.MessageMutator.IMutateIncomingTransportMessages, NServiceBus.MessageMutator.IMutateOutgoingMessages or NServiceBus.MessageMutator.IMutateOutgoingTransportMessages"));
    }

    [TestCase(typeof(IncomingMessageMutator))]
    [TestCase(typeof(IncomingTransportMessageMutator))]
    [TestCase(typeof(OutgoingMessageMutator))]
    [TestCase(typeof(OutgoingTransportMessageMutator))]
    public void Should_not_throw_when_registering_mutator(Type mutatorType)
    {
        var endpointConfiguration = new EndpointConfiguration("test");
        var messageMutator = Activator.CreateInstance(mutatorType);

        Assert.DoesNotThrow(() => endpointConfiguration.RegisterMessageMutator(messageMutator));
    }

    [TestCase(typeof(IncomingMessageMutator))]
    [TestCase(typeof(IncomingTransportMessageMutator))]
    [TestCase(typeof(OutgoingMessageMutator))]
    [TestCase(typeof(OutgoingTransportMessageMutator))]
    public void Should_only_invoke_instances_once_even_if_registered_multiple_times(Type mutatorType)
    {
        var endpointConfiguration = new EndpointConfiguration("test");
        var messageMutator = Activator.CreateInstance(mutatorType);

        endpointConfiguration.RegisterMessageMutator(messageMutator);
        endpointConfiguration.RegisterMessageMutator(messageMutator);

        var registry = endpointConfiguration.Settings.Get<NServiceBus.Features.Mutators.RegisteredMutators>();

        if (mutatorType == typeof(IncomingMessageMutator))
        {
            Assert.That(registry.IncomingMessage.Count, Is.EqualTo(1));
        }

        if (mutatorType == typeof(IncomingTransportMessageMutator))
        {
            Assert.That(registry.IncomingTransportMessage.Count, Is.EqualTo(1));
        }

        if (mutatorType == typeof(OutgoingMessageMutator))
        {
            Assert.That(registry.OutgoingMessage.Count, Is.EqualTo(1));
        }

        if (mutatorType == typeof(OutgoingTransportMessageMutator))
        {
            Assert.That(registry.OutgoingTransportMessage.Count, Is.EqualTo(1));
        }
    }

    [Test]
    public void Should_not_throw_when_registering_mutator_implementing_multiple_mutator_interfaces()
    {
        var endpointConfiguration = new EndpointConfiguration("test");

        Assert.DoesNotThrow(() => endpointConfiguration.RegisterMessageMutator(new MultiMutator()));
    }

    class IncomingMessageMutator : IMutateIncomingMessages
    {
        public Task MutateIncoming(MutateIncomingMessageContext context)
        {
            throw new NotImplementedException();
        }
    }

    class IncomingTransportMessageMutator : IMutateIncomingTransportMessages
    {
        public Task MutateIncoming(MutateIncomingTransportMessageContext context)
        {
            throw new NotImplementedException();
        }
    }

    class OutgoingMessageMutator : IMutateOutgoingMessages
    {
        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
            throw new NotImplementedException();
        }
    }

    class OutgoingTransportMessageMutator : IMutateOutgoingTransportMessages
    {
        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            throw new NotImplementedException();
        }
    }

    class MultiMutator : IMutateIncomingMessages, IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages, IMutateOutgoingMessages
    {
        public Task MutateIncoming(MutateIncomingMessageContext context)
        {
            throw new NotImplementedException();
        }

        public Task MutateIncoming(MutateIncomingTransportMessageContext context)
        {
            throw new NotImplementedException();
        }

        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            throw new NotImplementedException();
        }

        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
            throw new NotImplementedException();
        }
    }
}