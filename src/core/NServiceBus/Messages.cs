using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Messages
{
    [Serializable]
    public class ReadyMessage : IMessage 
    {
        private bool clearPreviousFromThisAddress;
        public bool ClearPreviousFromThisAddress
        {
            get { return clearPreviousFromThisAddress; }
            set { clearPreviousFromThisAddress = value; }
        }
    }
}
