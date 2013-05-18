namespace NServiceBus.Gateway.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Provides the basic functionality to persist Gateway messages.
    /// </summary>
    [ObsoleteEx(Replacement = "IDeduplicateMessages", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public interface IPersistMessages
    {
        /// <summary>
        /// When implemented in a class, stores a gateway message.
        /// </summary>
        /// <param name="clientId">Message identifier.</param>
        /// <param name="timeReceived">Message time received.</param>
        /// <param name="message">The Message.</param>
        /// <param name="headers">Ant associated message headers.</param>
        /// <returns><c>true</c> if success, otherwise <c>false</c>.</returns>
        bool InsertMessage(string clientId, DateTime timeReceived, Stream message, IDictionary<string,string> headers);

        /// <summary>
        /// When implemented in a class, updates the message with a status of acknowledged.
        /// </summary>
        /// <param name="clientId">Message identifier.</param>
        /// <param name="message">The Message.</param>
        /// <param name="headers">Ant associated message headers.</param>
        /// <returns><c>true</c> if success, otherwise <c>false</c>.</returns>
        bool AckMessage(string clientId, out byte[] message, out  IDictionary<string, string> headers);

        /// <summary>
        /// When implemented in a class, updates the message headers.
        /// </summary>
        /// <param name="clientId">Message identifier.</param>
        /// <param name="headerKey">Header key to update.</param>
        /// <param name="newValue">New value.</param>
        void UpdateHeader(string clientId, string headerKey, string newValue);
    }
}