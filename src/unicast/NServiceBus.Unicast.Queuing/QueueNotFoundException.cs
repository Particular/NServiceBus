using System;

namespace NServiceBus.Unicast.Queuing
{
    [Serializable]
    public class QueueNotFoundException : Exception
    {
        public string Queue { get; set; }

        public QueueNotFoundException()
        {
        }

        protected QueueNotFoundException(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            if (info != null)
                Queue = info.GetString("Queue");
        }

        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Queue", Queue);
        }
    }
}
