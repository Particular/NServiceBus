namespace NServiceBus
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public static class MessageSessionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static IMessageSessionRaw Raw(this IMessageSession session)
        {
            var raw = session as IMessageSessionRaw;
            if (raw == null)
            {
                throw new NotSupportedException();
            }
            return raw;
        }
    }
}