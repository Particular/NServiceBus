using NServiceBus;
using System;

namespace MyMessages
{
   [Serializable]
   public class RequestDataMessage : IMessage
   {
      public bool Fault { get; set; }
   }

   [Serializable]
   public class DataResponseMessage : IMessage
   {
      public bool Fault { get; set; }
   }
}
