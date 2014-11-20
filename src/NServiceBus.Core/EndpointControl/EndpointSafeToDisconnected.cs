namespace NServiceBus.EndpointControl
{
    using System.Collections.Generic;

    /// <summary>
    /// Event emitted when the endpoints input queue is drained and its ready to be disconnected/shutdown
    /// </summary>
    public class EndpointSafeToDisconnected
    {
        readonly Dictionary<string, string> headers;
        
        /// <summary>
        /// Creates a new instance of <see cref="EndpointSafeToDisconnected"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        public EndpointSafeToDisconnected(Dictionary<string, string> headers)
        {
            this.headers = headers;
        }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get { return headers; } }
    }
}