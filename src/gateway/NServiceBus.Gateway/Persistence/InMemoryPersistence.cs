namespace NServiceBus.Gateway.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;

    public class InMemoryPersistence : IPersistMessages
    {
        readonly IList<MessageInfo> storage = new List<MessageInfo>();

        public bool InsertMessage(DateTime dateTime, string clientId, byte[] md5, byte[] message, NameValueCollection headers)
        {
           if (md5.Length != 16)
                throw new ArgumentException("md5 must be 16 bytes.");

            lock(storage)
            {
                if (storage.Any(m => m.ClientId == clientId && m.MD5 == md5))
                    return false;


                storage.Add(new MessageInfo
                                {
                                    ClientId = clientId,
                                    MD5 = md5,
                                    At = dateTime,
                                    Message = message,
                                    Headers = headers
                                });
            }

            return true;
        }

        public void AckMessage(string clientId, byte[] md5, out byte[] message, out NameValueCollection headers)
        {
            if (md5.Length != 16)
                throw new ArgumentException("md5 must be 16 bytes.");

            message = null;
            headers = null;

            lock(storage)
            {
                //todo - figure out why the md5 comparison isn't working
                var messageToAck =
                    storage.FirstOrDefault(m => !m.Acknowledged && m.ClientId == clientId);//&& m.MD5 == md5);

                if (messageToAck == null)
                    return;

                messageToAck.Acknowledged = true;

                message = messageToAck.Message;
                headers = messageToAck.Headers;
            }
        }

        public int DeleteDeliveredMessages(DateTime until)
        {
            return 0;//todo
        }
    }

    internal class MessageInfo
    {
        public string ClientId { get; set; }

        public byte[] MD5 { get; set; }

        public DateTime At { get; set; }

        public byte[] Message { get; set; }

        public NameValueCollection Headers { get; set; }

        public bool Acknowledged { get; set; }
    }
}