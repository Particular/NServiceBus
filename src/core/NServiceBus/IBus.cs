using System;
using NServiceBus.Async;

namespace NServiceBus
{
    public interface IBus
    {
        void Start();

        void Publish(IMessage message);
        /// <summary>
        /// Publishes to all subscribers of the first message in the list.
        /// </summary>
        /// <param name="messages"></param>
        void Publish(params IMessage[] messages);

        void Subscribe(Type messageType);
        void Subscribe(Type messageType, Predicate<IMessage> condition);
        void Unsubscribe(Type messageType);

        void Send(IMessage message);

        /// <summary>
        /// Sends to the destination configured for the first message in the list
        /// </summary>
        /// <param name="messages"></param>
        void Send(params IMessage[] messages);

        void SendLocal(params IMessage[] messages);

        void Send(IMessage message, string destination);

        /// <summary>
        /// Sends to the destination configured for the first message in the list
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="destination"></param>
        void Send(IMessage[] messages, string destination);

        void Send(IMessage message, CompletionCallback callback, object state);

        /// <summary>
        /// Sends to the destination configured for the first message in the list
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        void Send(IMessage[] messages, CompletionCallback callback, object state);

        void Send(IMessage message, string destination, CompletionCallback callback, object state);

        void Send(IMessage[] messages, string destination, CompletionCallback callback, object state);

        void Reply(IMessage message);

        /// <summary>
        /// Sends all messages to the destination found in <c>SourceOfMessageBeingHandled</c>
        /// </summary>
        /// <param name="messages"></param>
        void Reply(params IMessage[] messages);

        void Return(int errorCode);

        void HandleCurrentMessageLater();
        void HandleMessagesLater(params IMessage[] messages);
        void DoNotContinueDispatchingCurrentMessageToHandlers();

        /// <summary>
        /// Returns the queue from which the message being handled by the current thread originated.
        /// </summary>
        string SourceOfMessageBeingHandled { get; }
    }
}
