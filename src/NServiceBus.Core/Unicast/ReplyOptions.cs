namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Additional options that only apply for reply messages
    /// </summary>
    public class ReplyOptions : SendOptions
    {

        /// <summary>
        /// Both a destination and a correlation id is required when replying
        /// </summary>
        [ObsoleteEx(Replacement = "ReplyOptions(string destination, string correlationId)",RemoveInVersion = "7.0")]
        // ReSharper disable once UnusedParameter.Local
        public ReplyOptions(Address destination, string correlationId) : base(destination)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Both a destination and a correlation id is required when replying
        /// </summary>
        public ReplyOptions(string destination, string correlationId):base(destination)
        {
            if (destination == null)
            {
                throw new InvalidOperationException("Can't reply with null reply-to-address field. It can happen if you are using a SendOnly client.");
            }

            CorrelationId = correlationId;
        }
    }
}