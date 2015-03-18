namespace NServiceBus.Unicast.BackwardCompatibility
{
    using MessageMutator;
    using Serialization;
    using Transport;

    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Exist only for compatibility between V4 and V3. No longer needed in V6")]
    class MutateMessageContentTypeOfIncomingTransportMessages : IMutateIncomingTransportMessages, INeedInitialization
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
                transportMessage.Headers[Headers.ContentType] = Serializer.ContentType;
            }
        }

        public void Customize(BusConfiguration configuration)
        {
            configuration.RegisterComponents(c => c.ConfigureComponent<MutateMessageContentTypeOfIncomingTransportMessages>(DependencyLifecycle.InstancePerCall));
        }
    }
}