namespace NServiceBus.Unicast.Queuing
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Thrown when the queue could not be found.
    /// </summary>
    [Serializable]
    public class QueueNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="QueueNotFoundException" />.
        /// </summary>
        public QueueNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueueNotFoundException" />.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "QueueNotFoundException(string queue, string message, Exception inner)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        // ReSharper disable UnusedParameter.Local
        public QueueNotFoundException(Address queue, string message, Exception inner)
            // ReSharper restore UnusedParameter.Local
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueueNotFoundException" />.
        /// </summary>
        public QueueNotFoundException(string queue, string message, Exception inner) : base(message, inner)
        {
            Queue = queue;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueueNotFoundException" />.
        /// </summary>
        protected QueueNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Queue = info.GetString("Queue");
        }

        /// <summary>
        /// The queue address.
        /// </summary>
        public string Queue { get; set; }

        /// <summary>
        /// Gets the object data for serialization purposes.
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Queue", Queue);
        }
    }
}