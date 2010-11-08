using System;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.Forwarder
{
   [Serializable]
   public class ProcessingFailedMessage : IMessage
   {
      public TransportMessage FailedMessage { get; set; }
      public ExceptionInfo ExceptionInfo { get; set; }
   }
}