namespace NServiceBus.Unicast
{
    using System;

    partial class UnicastBusInternal
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="address">
        /// The address to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(Address address, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given address.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="address">The address to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends the message to the destination as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(string destination, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends the message to the given address as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send(object message, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send(Address address, string correlationId, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the destination identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the given address identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        [ObsoleteEx(
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor, SendOptions options)",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        public ICallback Defer(TimeSpan delay, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "SendLocal(object message, SendLocalOptions options)")]
        public ICallback Defer(DateTime processAt, object message)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", Message = "Replaced by NServiceBus.Callbacks package")]
        public void Return<T>(T errorEnum)
        {
            throw new NotImplementedException();
        }
    }
}