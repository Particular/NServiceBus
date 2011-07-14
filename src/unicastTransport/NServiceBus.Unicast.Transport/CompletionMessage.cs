using System;

namespace NServiceBus.Unicast.Transport
{
	/// <summary>
	/// A message that will be sent on completion or error of an NServiceBus message.
	/// </summary>
    [Serializable]
    public class CompletionMessage : IMessage
    {
		/// <summary>
		/// Gets/sets a code specifying the type of error that occurred.
		/// </summary>
        public int ErrorCode { get; set; }
    }
}
