using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Async
{
	/// <summary>
	/// A delegate for a method that will be called back on completion of a message.
	/// </summary>
	/// <param name="errorCode">The result code of the message.</param>
	/// <param name="state">An object that can contain state information for the method.</param>
    public delegate void CompletionCallback(int errorCode, object state);
}
