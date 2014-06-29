namespace NServiceBus.Unicast.Queuing
{
    using System;

    /// <summary>
    /// Thrown when the queue could not be found
    /// </summary>
    [Serializable]
    public class QueueNotFoundException : Exception
    {
        /// <summary>
        /// The queue address
        /// </summary>
        public Address Queue { get; set; }

        /// <summary>
        /// Ctor
        /// </summary>
        public QueueNotFoundException()
        {
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public QueueNotFoundException(Address queue, string message, Exception inner) : base( message, inner )
        {
            Queue = queue;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected QueueNotFoundException(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            if (info != null)
                Queue = Address.Parse(info.GetString("Queue"));
        }

        /// <summary>
        /// Gets the object data for serialization purposes
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Queue", Queue.ToString());
        }
    }
}
