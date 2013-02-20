namespace NServiceBus
{
    ///<summary>
    /// Enumeration defining different kinds of message intent like Send and Publish.
    ///</summary>
    public enum MessageIntentEnum
    {
        /// <summary>
        /// Initialization - Here just for compatibility with NServiceBus V2.6
        /// </summary>
        Init = 0,
        
        ///<summary>
        /// Regular point-to-point send
        ///</summary>
        Send = 1,

        ///<summary>
        /// Publish, not a regular point-to-point send
        ///</summary>
        Publish = 2,

        /// <summary>
        /// Subscribe
        /// </summary>
        Subscribe = 3,

        /// <summary>
        /// Unsubscribe
        /// </summary>
        Unsubscribe = 4,
    }
}
