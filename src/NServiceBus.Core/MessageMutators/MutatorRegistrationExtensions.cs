namespace NServiceBus.MessageMutator
{
    using System;

    /// <summary>
    /// Provides extension methods to register message mutators.
    /// </summary>
    public static class MutatorRegistrationExtensions
    {
        /// <summary>
        /// Register an incoming message mutator with the pipeline.
        /// </summary>
        /// <param name="endpointConfiguration">The configuration to extend.</param>
        /// <param name="messageMutator">An instance of <see cref="IMutateIncomingMessages"/>, <see cref="IMutateIncomingTransportMessages"/>, <see cref="IMutateOutgoingMessages"/> or <see cref="IMutateOutgoingTransportMessages"/>.</param>
        public static void RegisterMessageMutator(this EndpointConfiguration endpointConfiguration, object messageMutator)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(messageMutator), messageMutator);

            if (!IsMessageMutator(messageMutator))
            {
                throw new ArgumentException($"The given instance is no valid message mutator. Implement one of the following mutator interfaces: {typeof(IMutateIncomingMessages).FullName}, {typeof(IMutateIncomingTransportMessages).FullName}, {typeof(IMutateOutgoingMessages).FullName}, {typeof(IMutateOutgoingTransportMessages).FullName}");
            }

            endpointConfiguration.RegisterComponents(c => c.RegisterSingleton(messageMutator));
        }

        static bool IsMessageMutator(object messageMutatorType)
        {
            return messageMutatorType is IMutateIncomingMessages
                   || messageMutatorType is IMutateIncomingTransportMessages
                   || messageMutatorType is IMutateOutgoingMessages
                   || messageMutatorType is IMutateOutgoingTransportMessages;
        }
    }
}