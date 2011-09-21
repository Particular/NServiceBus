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
                if (Bus.CurrentMessageContext.Headers.ContainsKey(key))
                    return Bus.CurrentMessageContext.Headers[key];
                else
                    return null;

            if (Bus.OutgoingHeaders.ContainsKey(key))
                return Bus.OutgoingHeaders[key];

            return null;
        }

        /// <summary>
        /// If the source of this message was an Http endpoint, returns its address
        /// otherwise returns null.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string GetHttpFromHeader(this IMessage msg)
        {
            return msg.GetHeader(Headers.HttpFrom);
        }

        /// <summary>
        /// If the target destination of this message is an Http endpoint,
        /// return the address of that target, otherwise null.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string GetHttpToHeader(this IMessage msg)
        {
            return msg.GetHeader(Headers.HttpTo);
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
            else
                Bus.OutgoingHeaders[key] = value;
        }

        /// <summary>
        /// Sets the Http address from which this message was received.
        /// This method is reserved for the NServiceBus Gateway.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="value"></param>
        public static void SetHttpFromHeader(this IMessage msg, string value)
        {
            msg.SetHeader(Headers.HttpFrom, value);
        }

        /// <summary>
        /// Sets the Http address to which this message should be sent.
        /// Requires the use of the NServiceBus Gateway.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="value"></param>
        public static void SetHttpToHeader(this IMessage msg, string value)
        {
            msg.SetHeader(Headers.HttpTo, value);
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

            if (Bus.CurrentMessageContext.Headers.ContainsKey(key))
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
		public static IMessage CurrentMessageBeingHandled 
		{ 
			get
			{
				return _currentMessageBeingHandled;
			} 
			set
			{
				_currentMessageBeingHandled = value;
			}
		}
		
		[ThreadStatic]
		static IMessage _currentMessageBeingHandled;
    }

    /// <summary>
    /// Static class containing headers used by NServiceBus.
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// Header for retrieving from which Http endpoint the message arrived.
        /// </summary>
        public const string HttpFrom = "NServiceBus.From";

        /// <summary>
        /// Header for specifying to which Http endpoint the message should be delivered.
        /// </summary>
        public const string HttpTo = "NServiceBus.To";
    }
}
