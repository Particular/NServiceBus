namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using MessageMutator;

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
            transportMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<SentTimeMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}