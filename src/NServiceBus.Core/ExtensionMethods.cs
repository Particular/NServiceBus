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
            return GetHeaderAction(msg, key);
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/>.</param>
        /// <param name="msg">The message to add a header to.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The value to assign to the header.</param>
        public static void SetMessageHeader(this IBus bus, object msg, string key, string value)
        {
            SetHeaderAction(msg, key, value);
        }

        /// <summary>
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        /// <param name="msg">The <see cref="IMessage"/> to retrieve a header from.</param>
        /// <param name="key">The header key.</param>
        /// <returns>The value assigned to the header.</returns>
        public static string GetHeader(this IMessage msg, string key)
        {
            return GetHeaderAction(msg, key);
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="msg">The <see cref="IMessage"/> to add a header to.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The value to assign to the header.</param>
        public static void SetHeader(this IMessage msg, string key, string value)
        {
            SetHeaderAction(msg, key, value);
        }


        /// <summary>
        /// The <see cref="IBus"/> used by the extension methods for accessing headers.
        /// </summary>
        public static IBus Bus { get; set; }

        /// <summary>
        /// The object used to see whether headers requested are for the handled message.
        /// </summary>
        public static object CurrentMessageBeingHandled { get { return currentMessageBeingHandled; } set { currentMessageBeingHandled = value; } }

        [ThreadStatic]
        static object currentMessageBeingHandled;

        /// <summary>
        /// The <see cref="Action{T1,T2,T3}"/> used to set the header in the <see cref="SetHeader(NServiceBus.IMessage,string,string)"/> method.
        /// </summary>
        public static Action<object, string, string> SetHeaderAction = (x, y, z) =>
                                                                           {
                                                                               //default to no-op to avoid getting in the way of unit testing
                                                                           };

        /// <summary>
        /// The <see cref="Func{T1,T2,TResult}"/> used to get the header value in the <see cref="GetHeader(NServiceBus.IMessage,string)"/> method.
        /// </summary>
        public static Func<object, string, string> GetHeaderAction = (x, y) => "No header get header action was defined, please specify one using ExtensionMethods.GetHeaderAction = ...";

    }
}
