using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.Forwarder
{
   public class FaultManager : IManageMessageFailures
   {      
      public void SerializationFailedForMessage(TransportMessage message, Exception e)
      {
         Bus.Send(AggregatorEndpoint, new SerializationFailedMessage
         {
           ExceptionInfo = e.GetInfo(),
           FailedMessage = message
         });
      }

      public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
      {
         if (SanitizeProcessingExceptions)
         {
            e = ExceptionSanitizer.Sanitize(e);
         }
         Bus.Send(AggregatorEndpoint, new ProcessingFailedMessage
         {
            ExceptionInfo = e.GetInfo(),
            FailedMessage = message
         });
      }

      public IBus Bus { get; set; }
      public string AggregatorEndpoint { get; set; }
      public bool SanitizeProcessingExceptions { get; set; }
   }
}
