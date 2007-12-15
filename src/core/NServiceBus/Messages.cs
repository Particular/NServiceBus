using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Messages
{
	/// <summary>
	/// Defines a message indicating that a transport is ready to
	/// receive a message.
	/// </summary>
    [Serializable]
    public class ReadyMessage : IMessage 
    {
        private bool clearPreviousFromThisAddress;

		/// <summary>
		/// Gets/sets whether or not previous ready messages from the same
		/// sender should be cleared.
		/// </summary>
        public bool ClearPreviousFromThisAddress
        {
            get { return clearPreviousFromThisAddress; }
            set { clearPreviousFromThisAddress = value; }
        }
    }
}
