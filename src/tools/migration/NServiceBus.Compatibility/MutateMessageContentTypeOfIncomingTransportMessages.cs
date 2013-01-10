﻿namespace NServiceBus.Compatibility
{
    using NServiceBus.MessageMutator;
    using NServiceBus.Serialization;

    using NServiceBus.Unicast.Transport;

    public class MutateMessageContentTypeOfIncomingTransportMessages : IMutateIncomingTransportMessages, INeedInitialization
    {
        public IMessageSerializer Serializer { get; set; }

        /// <summary>
        /// Ensure that the content type which is introduced in V4.0.0 and later versions is present in the header.
        /// </summary>
        /// <param name="transportMessage">Transport Message to mutate.</param>
        public void MutateIncoming(TransportMessage transportMessage)
        {
            if (!transportMessage.IsControlMessage() && !transportMessage.Headers.ContainsKey(Headers.ContentType))
            {
                transportMessage.Headers[Headers.ContentType] = this.Serializer.ContentType;
            }
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MutateMessageContentTypeOfIncomingTransportMessages>(DependencyLifecycle.InstancePerCall);
        }
    }
}