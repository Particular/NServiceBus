namespace NServiceBus.Unicast.Timing
{
    using System;
    using Config;
    using MessageMutator;
    using Transport;

    /// <summary>
    /// Set the TimeSent header
    /// </summary>
    public class SentTimeMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Stamps the message with the current time in UTC
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers[Headers.TimeSent] = DateTime.UtcNow.ToWireFormattedString();
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<SentTimeMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}