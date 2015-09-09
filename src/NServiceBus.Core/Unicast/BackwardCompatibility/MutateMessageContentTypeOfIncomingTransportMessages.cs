namespace NServiceBus
{
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Unicast.Transport;
    using Serialization;
    
    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Exist only for compatibility between V4 and V3. No longer needed in V6")]
    class MutateMessageContentTypeOfIncomingTransportMessages : IMutateIncomingTransportMessages, INeedInitialization
    {
        public IMessageSerializer Serializer { get; set; }

        /// <summary>
        /// Ensure that the content type which is introduced in V4.0.0 and later versions is present in the header.
        /// </summary>
        /// <param name="transportMessage">Transport Message to mutate.</param>
        public Task MutateIncoming(MutateIncomingTransportMessageContext transportMessage)
        {
            var headers = transportMessage.Headers;
            if (!TransportMessageExtensions.IsControlMessage(headers) && !headers.ContainsKey(Headers.ContentType))
            {
                headers[Headers.ContentType] = Serializer.ContentType;
            }
            return Task.FromResult(0);
        }

        public void Customize(BusConfiguration configuration)
        {
            configuration.RegisterComponents(c => c.ConfigureComponent<MutateMessageContentTypeOfIncomingTransportMessages>(DependencyLifecycle.InstancePerCall));
        }
    }
}