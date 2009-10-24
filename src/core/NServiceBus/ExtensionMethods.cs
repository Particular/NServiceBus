using System;
using System.Collections.Generic;

namespace NServiceBus
{
    /// <summary>
    /// Class containing extension methods for base class libraries for using interface-based messages.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Instantiates an instance of T and adds it to the list.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <param name="list">The list to which to add the new element</param>
        /// <param name="constructor">An action for setting properties of the created instance.</param>
        public static void Add<T>(this IList<T> list, Action<T> constructor) where T : IMessage
        {
            if (MessageCreator == null)
                throw new InvalidOperationException("MessageCreator has not been set.");

            list.Add(MessageCreator.CreateInstance(constructor));
        }

        /// <summary>
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetHeader(this IMessage msg, string key)
        {
            if (msg == CurrentMessageBeingHandled)
                return Bus.CurrentMessageContext.Headers[key];

            return Bus.OutgoingHeaders[key];
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetHeader(this IMessage msg, string key, string value)
        {
            if (msg == CurrentMessageBeingHandled)
                Bus.CurrentMessageContext.Headers[key] = value;

            Bus.OutgoingHeaders[key] = value;
        }

        /// <summary>
        /// Gets the value of the header with the given key and sets it for this message.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        public static void CopyHeaderFromRequest(this IMessage msg, string key)
        {
            if (msg == CurrentMessageBeingHandled)
                throw new InvalidOperationException("This method is not supported on the request message.");

            Bus.OutgoingHeaders[key] = Bus.CurrentMessageContext.Headers[key];
        }

        /// <summary>
        /// The object used by the extention methods to instantiate types.
        /// </summary>
        public static IMessageCreator MessageCreator { get; set; }

        /// <summary>
        /// The object used by the extension methods for accessing headers.
        /// </summary>
        public static IBus Bus { get; set; }

        /// <summary>
        /// The object used to see whether headers requested are for the handled message.
        /// </summary>
        public static IMessage CurrentMessageBeingHandled { get; set; }
    }
}
