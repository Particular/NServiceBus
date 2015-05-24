namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Defines an event handler.
    /// </summary>
    /// <typeparam name="T">The type of event to be handled.</typeparam>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IProcessEvents<T>
    {
        /// <summary>
        /// Handles an event.
        /// </summary>
        /// <param name="message">The event to handle.</param>
        /// <param name="context">The event context</param>
        /// <remarks>
        /// This method will be called when an event arrives on the bus and should contain
        /// the custom logic to execute when the event is received.</remarks>
        void Handle(T message, IEventContext context);
    }
}