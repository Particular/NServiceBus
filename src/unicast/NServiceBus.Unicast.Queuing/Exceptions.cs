using System;

namespace NServiceBus.Unicast.Queuing
{
    public class QueueNotFoundException : Exception
    {
        public string Queue { get; set; }
    }
}
