using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace NServiceBus.Unicast.Transport
{
    [Serializable]
    public class Msg
    {
        private string id;
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        private string correlationId;
        public string CorrelationId
        {
            get { return correlationId; }
            set { correlationId = value; }
        }

        private string returnAddress;
        public string ReturnAddress
        {
            get { return returnAddress; }
            set { returnAddress = value; }
        }

        private string windowsIdentityName;
        public string WindowsIdentityName
        {
            get { return windowsIdentityName; }
            set { windowsIdentityName = value; }
        }

        private bool recoverable;
        public bool Recoverable
        {
            get { return recoverable; }
            set { recoverable = value; }
        }

        private TimeSpan timeToBeReceived = TimeSpan.MaxValue;
        public TimeSpan TimeToBeReceived
        {
            get { return timeToBeReceived; }
            set { timeToBeReceived = value; }
        }

        private IMessage[] body;
        [XmlIgnore]
        public IMessage[] Body
        {
            get { return body; }
            set { body = value; messages = new List<object>(body); }
        }

        private Stream bodyStream;
        /// <summary>
        /// Used for cases where we can't deserialize the contents.
        /// </summary>
        [XmlIgnore]
        public Stream BodyStream
        {
            get { return bodyStream; }
            set { bodyStream = value; }
        }

        private List<object> messages;
        public List<object> Messages
        {
            get { return messages; }
            set { messages = value; }
        }

        public void CopyMessagesToBody()
        {
            this.body = new IMessage[this.messages.Count];
            this.messages.CopyTo(this.body);
        }
    }
}
