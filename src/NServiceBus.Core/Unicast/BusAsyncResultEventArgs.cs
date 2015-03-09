namespace NServiceBus
{
    using System;
    using NServiceBus.Unicast;

    /// <summary>
    /// Argument passed in the Registered event of the Callback object.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "6.0", RemoveInVersion = "7.0")]
    public class BusAsyncResultEventArgs : EventArgs
    {
        /// <summary>
        /// Gets/sets the IAsyncResult.
        /// </summary>
        public BusAsyncResult Result { get; set; }

        /// <summary>
        /// Gets/sets the message id.
        /// </summary>
        public string MessageId { get; set; }
    }
}