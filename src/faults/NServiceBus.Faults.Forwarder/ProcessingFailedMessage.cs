using System;
using System.Linq;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.Forwarder
{
   [Serializable]
   public class ProcessingFailedMessage : IMessage
   {
      public TransportMessage FailedMessage { get; set; }
      public Exception Exception { get; set; }
   }
}