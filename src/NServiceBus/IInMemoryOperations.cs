namespace NServiceBus
{
    using System;

    /// <summary>
    /// In memory operations
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Removed to reduce complexity and API confusion. See https://github.com/Particular/NServiceBus/issues/1983 for more information.")]
    public interface IInMemoryOperations
    {
        /// <summary>
        /// Raises an in memory event
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="event">The message to raise</param>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Removed to reduce complexity and API confusion. See https://github.com/Particular/NServiceBus/issues/1983 for more information.")]
        void Raise<T>(T @event);

        /// <summary>
        /// Instantiates a message of type <typeparamref name="T"/> and raise it in memory
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An <see cref="Action{T}"/> which initializes properties of the message</param>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Removed to reduce complexity and API confusion. See https://github.com/Particular/NServiceBus/issues/1983 for more information.")]
        void Raise<T>(Action<T> messageConstructor);
    }
}