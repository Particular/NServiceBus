namespace NServiceBus.MessageMutator
{
    using System;
    using System.Collections.Generic;

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
            foreach (var mutatorInterface in GetImplementedMutatorInterfaces(messageMutator))
            {
                // register the mutator with its specific mutator interface as it will be registered as object instead
                endpointConfiguration.RegisterComponents(c => c.RegisterSingleton(mutatorInterface, messageMutator));
                registeredMutator = true;
            }

            if (!registeredMutator)
            {
                throw new ArgumentException($"The given instance is not a valid message mutator. Implement one of the following mutator interfaces: {typeof(IMutateIncomingMessages).FullName}, {typeof(IMutateIncomingTransportMessages).FullName}, {typeof(IMutateOutgoingMessages).FullName} or {typeof(IMutateOutgoingTransportMessages).FullName}");
            }
        }

        static IEnumerable<Type> GetImplementedMutatorInterfaces(object messageMutatorType)
        {
            if (messageMutatorType is IMutateIncomingMessages)
            {
                yield return typeof(IMutateIncomingMessages);
            }

            if (messageMutatorType is IMutateIncomingTransportMessages)
            {
                yield return typeof(IMutateIncomingTransportMessages);
            }

            if (messageMutatorType is IMutateOutgoingMessages)
            {
                yield return typeof(IMutateOutgoingMessages);
            }

            if (messageMutatorType is IMutateOutgoingTransportMessages)
            {
                yield return typeof(IMutateOutgoingTransportMessages);
            }
        }
    }
}