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
        /// The object used by the extention methods to instantiate types.
        /// </summary>
        public static IMessageCreator MessageCreator { get; set; }
    }
}
