namespace NServiceBus.Unicast.Queuing.Azure
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.WindowsAzure.Storage.Queue;

    public class EnvelopeDeserializationFailed:SerializationException
    {
        CloudQueueMessage message;


        public EnvelopeDeserializationFailed(CloudQueueMessage message, Exception ex)
            : base("Failed to deserialize message envelope", ex)
        {
            this.message = message;
        }

        public CloudQueueMessage Message
        {
            get { return message; }
        }
    }
}