namespace NServiceBus
{
    using System;

    /// <summary>
    /// <see cref="IBus"/> implementers should also implement this interface to support get/set of headers for current message.
    /// </summary>
    public interface IManageMessageHeaders
    {
        /// <summary>
        /// The <see cref="Action{T1,T2,T3}"/> used to set the header in the bus.SetMessageHeader(msg, key, value) method.
        /// </summary>
        Action<object, string, string> SetHeaderAction { get; }
        /// <summary>
        /// The <see cref="Func{T1,T2,TResult}"/> used to get the header value in the bus.GetMessageHeader(msg, key) method.
        /// </summary>
        Func<object, string, string> GetHeaderAction { get; }
    }
}