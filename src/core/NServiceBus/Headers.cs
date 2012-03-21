namespace NServiceBus
{
    /// <summary>
    /// Static class containing headers used by NServiceBus.
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// Header for retrieving from which Http endpoint the message arrived.
        /// </summary>
        public const string HttpFrom = "NServiceBus.From";

        /// <summary>
        /// Header for specifying to which Http endpoint the message should be delivered.
        /// </summary>
        public const string HttpTo = "NServiceBus.To";

        /// <summary>
        /// Header for specifying to which queue behind the http gateway should the message be delivered.
        /// This header is considered an applicative header.
        /// </summary>
        public const string RouteTo = "NServiceBus.Header.RouteTo";

        /// <summary>
        /// Header for specifying to which sites the gateway should send the message. For multiple
        /// sites a comma separated list can be used
        /// This header is considered an applicative header.
        /// </summary>
        public const string DestinationSites = "NServiceBus.DestinationSites";

        /// <summary>
        /// Header for specifying the key for the site where this message originated. 
        /// This header is considered an applicative header.
        /// </summary>
        public const string OriginatingSite = "NServiceBus.OriginatingSite";

        /// <summary>
        /// Header for time when a message expires in the timeout manager
        /// This header is considered an applicative header.
        /// </summary>
        public const string Expire = "NServiceBus.Timeout.Expire";

        /// <summary>
        /// Header containing the id of the saga instance the sent the message
        /// This header is considered an applicative header.
        /// </summary>
        public const string SagaId = "NServiceBus.SagaId";

        /// <summary>
        /// Header telling the timeout manager to clear previous timeouts
        /// This header is considered an applicative header.
        /// </summary>
        public const string ClearTimeouts = "NServiceBus.ClearTimeouts";


        /// <summary>
        /// Prefix included on the wire when sending applicative headers.
        /// </summary>
        public const string HeaderName = "Header";
        
        /// <summary>
        /// Header containing the windows identity name
        /// </summary>
        public const string WindowsIdentityName = "WinIdName";

        /// <summary>
        /// Header telling the NServiceBus Version (beginning NServiceBus V3.0.1).
        /// </summary>
        public const string NServiceBusVersion = "NServiceBus.Version";
    }
}
