namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Serialization;

    /// <summary>
    /// An <see cref="IMessage"/> that is used by <see cref="ScheduleExtensions.ScheduleEvery(IMessageSession,TimeSpan,Func{IPipelineContext, Task})"/> and <see cref="ScheduleExtensions.ScheduleEvery(IMessageSession,TimeSpan, string ,Func{IPipelineContext, Task})"/>.
    /// </summary>
    /// <remarks>Allow implementations of <see cref="IMessageSerializer"/> to serialize and deserialize instances.</remarks>
    [Serializable]
    public class ScheduledTask : IMessage
    {

        /// <summary>
        /// The unique identifier.
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// The name to used for logging the task being executed.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The interval to repeatedly execute.
        /// </summary>
        public TimeSpan Every { get; set; }
    }
}