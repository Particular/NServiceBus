namespace NServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the transport to push messages to the core.
    /// </summary>
    public interface IPushMessages
    {
        /// <summary>
        /// Initializes the <see cref="IPushMessages"/>.
        /// </summary>
        DequeueInfo Init(Func<PushContext, Task> pipe, PushSettings settings);

        /// <summary>
        /// Starts pushing message/>.
        /// </summary>
        void Start(PushRuntimeSettings limitations);

        /// <summary>
        /// Stops pushing messages.
        /// </summary>
        void Stop();
    }
}