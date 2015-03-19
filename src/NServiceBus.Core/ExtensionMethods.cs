// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;


    /// <summary>
    /// Class containing extension methods for base class libraries for using interface-based messages.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/>.</param>
        /// <param name="message">The message to retrieve a header from.</param>
        /// <param name="key">The header key.</param>
        /// <returns>The value assigned to the header.</returns>
        public static string GetMessageHeader(this IBus bus, object message, string key)
        {
            Guard.AgainstDefault(bus, "bus");
            Guard.AgainstDefault(message, "message");
            Guard.AgainstDefaultOrEmpty(key, "key");
            var manageMessageHeaders = bus as IManageMessageHeaders;
            if (manageMessageHeaders != null)
            {
                return manageMessageHeaders.GetHeaderAction(message, key);
            }

            throw new InvalidOperationException("bus does not implement IManageMessageHeaders");
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/>.</param>
        /// <param name="message">The message to add a header to.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The value to assign to the header.</param>
        public static void SetMessageHeader(this ISendOnlyBus bus, object message, string key, string value)
        {
            Guard.AgainstDefault(bus, "bus");
            Guard.AgainstDefault(message, "message");
            Guard.AgainstDefaultOrEmpty(key, "key");
            var manageMessageHeaders = bus as IManageMessageHeaders;
            if (manageMessageHeaders != null)
            {
                manageMessageHeaders.SetHeaderAction(message, key, value);
                return;
            }

            throw new InvalidOperationException("bus does not implement IManageMessageHeaders");
        }

        /// <summary>
        /// The object used to see whether headers requested are for the handled message.
        /// </summary>
        public static object CurrentMessageBeingHandled
        {
            get { return currentMessageBeingHandled; }
            set
            {
                Guard.AgainstDefault(value, "value");
                currentMessageBeingHandled = value;
            }
        }

        [ThreadStatic]
        static object currentMessageBeingHandled;
    }
}
