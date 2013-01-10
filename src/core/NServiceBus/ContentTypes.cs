namespace NServiceBus
{
    /// <summary>
    /// Contains message body content type definitions.
    /// </summary>
    public static class ContentTypes
    {
        /// <summary>
        /// Indicates that the content type is "application/bson"
        /// </summary>
        public const string Bson = "application/bson";

        /// <summary>
        /// Indicates that the content type is "application/binary"
        /// </summary>
        public const string Binary = "application/binary";

        /// <summary>
        /// Indicates that the content type is "application/json"
        /// </summary>
        public const string Json = "application/json";

        /// <summary>
        /// Indicates that the content type is "text/xml"
        /// </summary>
        public const string Xml = "text/xml";
    }
}