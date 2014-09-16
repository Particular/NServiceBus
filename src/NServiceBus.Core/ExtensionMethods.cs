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
        /// <param name="msg">The message to retrieve a header from.</param>
        /// <param name="key">The header key.</param>
        /// <returns>The value assigned to the header.</returns>
        public static string GetMessageHeader(this IBus bus, object msg, string key)
        {
            var manageMessageHeaders = bus as IManageMessageHeaders;
            if (manageMessageHeaders != null)
            {
                return manageMessageHeaders.GetHeaderAction(msg, key);
            }

            throw new InvalidOperationException("bus does not implement IManageMessageHeaders");
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/>.</param>
        /// <param name="msg">The message to add a header to.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The value to assign to the header.</param>
        public static void SetMessageHeader(this ISendOnlyBus bus, object msg, string key, string value)
        {
            var manageMessageHeaders = bus as IManageMessageHeaders;
            if (manageMessageHeaders != null)
            {
                manageMessageHeaders.SetHeaderAction(msg, key, value);
                return;
            }

            throw new InvalidOperationException("bus does not implement IManageMessageHeaders");
        }

        /// <summary>
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        /// <param name="msg">The <see cref="IMessage"/> to retrieve a header from.</param>
        /// <param name="key">The header key.</param>
        /// <returns>The value assigned to the header.</returns>
        [ObsoleteEx(
            Replacement = "bus.GetMessageHeader(msg, key)", 
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static string GetHeader(this IMessage msg, string key)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="msg">The <see cref="IMessage"/> to add a header to.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The value to assign to the header.</param>
        [ObsoleteEx(
            Replacement = "bus.SetMessageHeader(msg, key, value)",
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static void SetHeader(this IMessage msg, string key, string value)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// The object used to see whether headers requested are for the handled message.
        /// </summary>
        public static object CurrentMessageBeingHandled { get { return currentMessageBeingHandled; } set { currentMessageBeingHandled = value; } }

        [ThreadStatic]
        static object currentMessageBeingHandled;

        

    }
}
