using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.Forwarder
{
   [Serializable]
   public class SerializationFailedMessage : IMessage
   {
      public TransportMessage FailedMessage { get; set; }
      public Exception Exception { get; set; }
   }
}