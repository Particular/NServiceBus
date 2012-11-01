// Completion message is used by a V3.X subscriber with a 2.6 publisher. 
// Do no change the namespace

namespace NServiceBus.Unicast.Transport
{
    using System;

    /// <summary>
	/// Used for compatibility with 2.6 endpoints.
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
