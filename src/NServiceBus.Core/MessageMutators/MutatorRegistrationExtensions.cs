﻿namespace NServiceBus.MessageMutator
{
    using Features;
    using System;

    /// <summary>
    /// Provides extension methods to register message mutators.
    /// </summary>
    public static class MutatorRegistrationExtensions
    {
        /// <summary>
        /// Register a message mutator with the pipeline.
        /// </summary>
        /// <param name="endpointConfiguration">The configuration to extend.</param>
        /// <param name="messageMutator">A mutator instance implementing <see cref="IMutateIncomingMessages"/>, <see cref="IMutateIncomingTransportMessages"/>, <see cref="IMutateOutgoingMessages"/> or <see cref="IMutateOutgoingTransportMessages"/>. The class can also implement multiple mutator interfaces at once.</param>
        public static void RegisterMessageMutator(this EndpointConfiguration endpointConfiguration, object messageMutator)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(messageMutator), messageMutator);

            var registeredMutator = false;

            var registry = endpointConfiguration.Settings.GetOrCreate<Mutators.RegisteredMutators>();

            if (messageMutator is IMutateIncomingMessages incomingMutator)
            {
                registry.IncomingMessage.Add(incomingMutator);
                registeredMutator = true;
            }

            if (messageMutator is IMutateIncomingTransportMessages incomingTransportMessageMutator)
            {
                registry.IncomingTransportMessage.Add(incomingTransportMessageMutator);
                registeredMutator = true;
            }

            if (messageMutator is IMutateOutgoingMessages outgoingMutator)
            {
                registry.OutgoingMessage.Add(outgoingMutator);
                registeredMutator = true;
            }

            if (messageMutator is IMutateOutgoingTransportMessages outgoingTransportMessageMutator)
            {
                registry.OutgoingTransportMessage.Add(outgoingTransportMessageMutator);
                registeredMutator = true;
            }

            if (!registeredMutator)
            {
                throw new ArgumentException($"The given instance is not a valid message mutator. Implement one of the following mutator interfaces: {typeof(IMutateIncomingMessages).FullName}, {typeof(IMutateIncomingTransportMessages).FullName}, {typeof(IMutateOutgoingMessages).FullName} or {typeof(IMutateOutgoingTransportMessages).FullName}");
            }
        }
    }
}