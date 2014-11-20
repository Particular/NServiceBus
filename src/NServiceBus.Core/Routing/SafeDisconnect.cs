namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Safe disconnect event data
    /// </summary>
    public class SafeDisconnect
    {
        readonly Dictionary<string, string> headers;
        
        /// <summary>
        /// Creates a new instance of <see cref="SafeDisconnect"/>.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        public SafeDisconnect(Dictionary<string, string> headers)
        {
            this.headers = headers;
        }

        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get { return headers; } }
    }
}