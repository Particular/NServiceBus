using System;
using NServiceBus.Saga;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Double-dispatch class.
    /// </summary>
    public class ReplyingToNullOriginatorDispatcher : IHandleReplyingToNullOriginator
    {
        /// <summary>
        /// Callback for when saga is trying to reply to an originator that is null.
        /// </summary>
        internal Action CallbackWhenReplyingToNullOriginator;

        /// <summary>
        /// Called when the user has tries to reply to a message with out a originator
        /// </summary>
        public void TriedToReplyToNullOriginator()
        {
            if (CallbackWhenReplyingToNullOriginator != null)
                CallbackWhenReplyingToNullOriginator();
        }


    }
}