namespace NServiceBus.Sagas
{
    /// <summary>
    /// Exposes meta information about how a given saga message is handled on the saga
    /// </summary>
    public enum SagaMessageHandledBy
    {
        /// <summary>
        /// Indicates that the saga message is a message which starts the saga representing the old style API
        /// </summary>
        StartedByMessage,

        /// <summary>
        /// Indicates that the saga message is a command which starts the saga saga representing the new style API
        /// </summary>
        StartedByCommand,

        /// <summary>
        /// Indicates that the saga message is an event which starts the saga representing the new style API
        /// </summary>
        StartedByEvent,

        /// <summary>
        /// Indicates that the saga message is a timeout message which is handled with the new style API
        /// </summary>
        ProcessTimeout,

        /// <summary>
        /// Indicates that the saga message is a timeout message which is handled with the old style API
        /// </summary>
        HandleTimeout,

        /// <summary>
        /// Indicates that the saga message is an event message which is handled with the new style API
        /// </summary>
        ProcessEvent,

        /// <summary>
        /// Indicates that the saga message is a command message which is handled with the new style API
        /// </summary>
        ProcessCommand,

        /// <summary>
        /// Indicates that the saga message is a message which is handled with the old style API
        /// </summary>
        HandleMessage
    }
}