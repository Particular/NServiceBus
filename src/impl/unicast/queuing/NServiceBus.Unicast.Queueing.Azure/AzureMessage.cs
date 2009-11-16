using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Unicast.Queueing.Azure
{
    [Serializable]
    public class AzureMessage
    {
        public AzureMessage()
        {
        }

        public AzureMessage(QueuedMessage message)
        {
            AppSpecific = message.AppSpecific;

            if (message.BodyStream != null)
            {
                message.BodyStream.Position = 0;

                Body = new byte[message.BodyStream.Length];
                message.BodyStream.Read(Body, 0, Body.Length);
            }

            CorrelationId = message.CorrelationId;
            Extension = message.Extension;
            Id = message.Id;
            Label = message.Label;
            LookupId = message.LookupId;
            Recoverable = message.Recoverable;
            ResponseQueue = message.ResponseQueue;
            TimeSent = message.TimeSent;
            TimeToBeReceived = message.TimeToBeReceived;
        }

        public TimeSpan TimeToBeReceived { get; set; }

        public DateTime TimeSent { get; set; }

        public string ResponseQueue { get; set; }

        public bool Recoverable { get; set; }

        public long LookupId { get; set; }

        public string Id { get; set; }

        public byte[] Extension { get; set; }

        public string CorrelationId { get; set; }

        public byte[] Body { get; set; }
        public string Label { get; set; }
        public int AppSpecific { get; set; }

        public QueuedMessage ToQueueMessage()
        {
            return new QueuedMessage
                       {
                           AppSpecific = AppSpecific,
                           BodyStream = (Body != null ? new MemoryStream(Body):null),
                           CorrelationId = CorrelationId,
                           Extension = Extension,
                           Id = Id,
                           Label = Label,
                           LookupId = LookupId,
                           Recoverable = Recoverable,
                           ResponseQueue = ResponseQueue,
                           TimeSent = TimeSent,
                           TimeToBeReceived = TimeToBeReceived,
                       };
        }

        public CloudQueueMessage ToNativeMessage()
        {
            
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                return new CloudQueueMessage(stream.ToArray());
            }

        }
    }
}