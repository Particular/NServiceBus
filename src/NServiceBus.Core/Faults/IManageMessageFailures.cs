namespace NServiceBus.Faults
{
    using System;

    /// <summary>
    /// Interface for defining how message failures will be handled.
    /// </summary>
    [ObsoleteEx(
       Message = "IManageMessageFailures is no longer an extension point. If you want full control over what happens when a message fails (including retries) please override the MoveFaultsToErrorQueue behavior. If you just want to get notified when messages are being moved please use BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e=>{}) ",
       RemoveInVersion = "7",
       TreatAsErrorFromVersion = "6")]
    public interface IManageMessageFailures
    {
        /// <summary>
        /// Invoked when the deserialization of a message failed.
        /// </summary>
        void SerializationFailedForMessage(TransportMessage message, Exception e);

        /// <summary>
        /// Invoked when a message has failed its processing the maximum number of time configured.
        /// </summary>
        void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e);

        /// <summary>
        /// Initializes the fault manager
        /// </summary>
        /// <param name="address">The address of the message source</param>
        void Init(Address address);
    }
}
