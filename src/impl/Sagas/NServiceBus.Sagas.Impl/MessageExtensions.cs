namespace NServiceBus.Sagas.Impl
{
    using System;
    using Saga;

    /// <summary>
    /// Helpers 
    /// </summary>
    public static class MessageExtensions
    {
        /// <summary>
        /// True if this is a timeout message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsTimeoutMessage(this object message)
        {
            return !string.IsNullOrEmpty(message.GetHeader(Headers.Expire)) && !string.IsNullOrEmpty(message.GetHeader(Headers.SagaId));
        }


        /// <summary>
        /// True if the timeout for this message has expired
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool TimeoutHasExpired(this object message)
        {
            var tm = message as TimeoutMessage;
            if (tm != null)
                return !tm.HasNotExpired();

            return DateTime.UtcNow >= DateTime.Parse(message.GetHeader(Headers.Expire));
        }


    }
}