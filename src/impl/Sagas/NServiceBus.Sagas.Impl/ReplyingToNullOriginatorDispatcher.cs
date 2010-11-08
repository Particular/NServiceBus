using System;
using NServiceBus.Saga;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Class used to bridge the dependency between Saga{T} in NServiceBus.dll and
    /// which doesn't have access to Common.Logging and the level of logging
    /// known in the Configure class found in this project in NServiceBus.Core.dll.
    /// </summary>
    public class ReplyingToNullOriginatorDispatcher : IHandleReplyingToNullOriginator
    {
        void IHandleReplyingToNullOriginator.TriedToReplyToNullOriginator()
        {
            if (Configure.Logger.IsDebugEnabled)
                throw new InvalidOperationException
                    (
                    "Originator of saga has not provided a return address - cannot reply.");
        }
    }
}