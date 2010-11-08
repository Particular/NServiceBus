namespace NServiceBus.Unicast.Transport
{
    ///<summary>
    /// Enumeration defining different kinds of message intent like Send and Publish.
    ///</summary>
    public enum MessageIntentEnum
    {
        /// <summary>
        /// Initialization
        /// </summary>
        Init,

        ///<summary>
        /// Regular point-to-point send
        ///</summary>
        Send,

        ///<summary>
        /// Publish, not a regular point-to-point send
        ///</summary>
        Publish,

        /// <summary>
        /// Subscribe
        /// </summary>
        Subscribe,

        /// <summary>
        /// Unsubscribe
        /// </summary>
        Unsubscribe
    }
}
