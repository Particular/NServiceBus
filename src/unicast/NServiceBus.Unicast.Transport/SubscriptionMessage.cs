using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Unicast.Transport
{
    [Serializable]
    public class SubscriptionMessage : IMessage
    {
        public SubscriptionMessage() { }
        public SubscriptionMessage(string typeName, SubscriptionType subscriptionType)
        {
            this.typeName = typeName;
            this.subscriptionType = subscriptionType;
        }

        public string typeName;
        public SubscriptionType subscriptionType;
    }

    public enum SubscriptionType { Add, Remove };
}
