namespace NServiceBus.Gateway.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    public class InMemoryPersistence : IPersistMessages
    {
        readonly IList<MessageInfo> storage = new List<MessageInfo>();

        public bool InsertMessage(string clientId, DateTime timeReceived, byte[] message, NameValueCollection headers)
        {
            lock(storage)
            {
                if (storage.Any(m => m.ClientId == clientId))
                    return false;


                storage.Add(new MessageInfo
                                {
                                    ClientId = clientId,
                                    At = timeReceived,
                                    Message = message,
                                    Headers = headers
                                });
            }

            return true;
        }

        public void AckMessage(string clientId, out byte[] message, out NameValueCollection headers)
        {
            message = null;
            headers = null;

            lock(storage)
            {
                var messageToAck =
                    storage.FirstOrDefault(m => !m.Acknowledged && m.ClientId == clientId);

                if (messageToAck == null)
                    return;

                messageToAck.Acknowledged = true;

                message = messageToAck.Message;
                headers = messageToAck.Headers;
            }
        }

        public void UpdateHeader(string clientId, string headerKey, string newValue)
        {
            lock (storage)
            {
                var message = storage.First(m => m.ClientId == clientId);


                message.Headers[headerKey] = newValue;
            }
        }

        public int DeleteDeliveredMessages(DateTime until)
        {
            lock(storage)
            {
                var toDelete = storage.Where(m => m.At < until);

                toDelete.ToList()
                    .ForEach(m=>storage.Remove(m));


                return toDelete.Count();
            }
        }
    }

    public class MessageInfo
    {
        public string ClientId { get; set; }

        public DateTime At { get; set; }

        public byte[] Message { get; set; }

        public NameValueCollection Headers { get; set; }

        public bool Acknowledged { get; set; }
    }
}