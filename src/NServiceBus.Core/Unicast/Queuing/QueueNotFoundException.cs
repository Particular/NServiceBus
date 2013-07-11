namespace NServiceBus.Unicast.Queuing
{
    using System;

    [Serializable]
    public class QueueNotFoundException : Exception
    {
        public Address Queue { get; set; }

        public QueueNotFoundException()
        {
        }
        public QueueNotFoundException(Address queue, string message, Exception inner) : base( message, inner )
        {
            Queue = queue;
        }

        protected QueueNotFoundException(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            if (info != null)
                Queue = Address.Parse(info.GetString("Queue"));
        }

        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Queue", Queue.ToString());
        }
    }
}
