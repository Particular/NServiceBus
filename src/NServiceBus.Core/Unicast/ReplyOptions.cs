namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Additional options that only apply for reply messages
    /// </summary>
    public partial class ReplyOptions : SendOptions
    {

        /// <summary>
        /// Both a destination and a correlation id is required when replying
        /// </summary>
        public ReplyOptions(string destination):base(destination)
        {
            if (destination == null)
            {
                throw new InvalidOperationException("Can't reply with null reply-to-address field. It can happen if you are using a SendOnly client.");
            }
        }
    }
}