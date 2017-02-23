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
        public static void RegisterMessageMutator<TMessageMutator>(this EndpointConfiguration endpointConfiguration)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);

            if (!IsMessageMutator(typeof(TMessageMutator)))
            {
                throw new ArgumentException($"The specified type is no valid message mutator. Implement one of the following mutator interfaces: {typeof(IMutateIncomingMessages).FullName}, {typeof(IMutateIncomingTransportMessages).FullName}, {typeof(IMutateOutgoingMessages).FullName}, {typeof(IMutateOutgoingTransportMessages).FullName}");
            }

            endpointConfiguration.RegisterComponents(c => c.ConfigureComponent<TMessageMutator>(DependencyLifecycle.InstancePerCall));
        }

        static bool IsMessageMutator(Type messageMutatorType)
        {
            return typeof(IMutateIncomingMessages).IsAssignableFrom(messageMutatorType)
                   || typeof(IMutateIncomingTransportMessages).IsAssignableFrom(messageMutatorType)
                   || typeof(IMutateOutgoingMessages).IsAssignableFrom(messageMutatorType)
                   || typeof(IMutateOutgoingTransportMessages).IsAssignableFrom(messageMutatorType);
        }
    }
}