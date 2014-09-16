namespace NServiceBus
{
    using System;

    /// <summary>
    /// Obsoleted IBus methods
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Placeholder for obsoletes")]
    public static class IBus_Obsoletes
    {
        // ReSharper disable UnusedParameter.Global

        /// <summary>
        /// Creates an instance of the message type T.
        /// </summary>
        /// <typeparam name="T">The type of message interface to instantiate.</typeparam>
        /// <returns>A message object that implements the interface T.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Since multi message sends is obsoleted in v5 use `IBus.Send<T>()` instead")]
        public static T CreateInstance<T>(this IBus bus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an instance of the message type T and fills it with data.
        /// </summary>
        /// <typeparam name="T">The type of message interface to instantiate.</typeparam>
        /// <param name="bus">The bus</param>
        /// <param name="action">An action to set various properties of the instantiated object.</param>
        /// <returns>A message object that implements the interface T.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Since multi message sends is obsoleted in v5 use `IBus.Send<T>()` instead")]
        public static T CreateInstance<T>(this IBus bus, Action<T> action)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an instance of the given message type.
        /// </summary>
        /// <param name="bus">The bus</param>
        /// <param name="messageType">The type of message to instantiate.</param>
        /// <returns>A message object that implements the given interface.</returns>
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "Since multi message sends is obsoleted in v5 use `IBus.Send<T>()` instead")]
        public static object CreateInstance(this IBus bus, Type messageType)
        {
            throw new NotImplementedException();
        }

        // ReSharper restore UnusedParameter.Global

    }
}