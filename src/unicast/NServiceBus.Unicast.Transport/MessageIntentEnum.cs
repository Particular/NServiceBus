namespace NServiceBus.Unicast.Transport
{
    ///<summary>
    /// Enumeration defining different kinds of message intent like Send and Publish.
    ///</summary>
    public enum MessageIntentEnum
    {
        ///<summary>
        /// Indicates that the intent is to do a regular point-to-point send
        ///</summary>
        Send,

        ///<summary>
        /// Indicates that the intent is to do a publish, not a regular point-to-point send
        ///</summary>
        Publish
    }
}
