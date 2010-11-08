using System;

namespace NServiceBus.Unicast.Transport
{
    /// <summary>
    /// An exception thrown when applicative code requests abort the
    /// handling of the current message.
    /// The reason this is modeled as an exception is to cause
    /// existing transaction scopes to rollback.
    /// </summary>
    public class AbortHandlingCurrentMessageException : Exception
    {
    }
}
