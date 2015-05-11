namespace NServiceBus
{
    using System;

    internal class EventContext : IEventContext
    {
        readonly IBus bus;

        public EventContext(IBus bus)
        {
            this.bus = bus;
        }

        public void Publish(object message, PublishOptions options)
        {
            bus.Publish(message, options);
        }

        public void Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            bus.Publish(messageConstructor, publishOptions);
        }

        public void Send(object message, SendOptions options)
        {
            bus.Send(message, options);
        }

        public void Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            bus.Send(messageConstructor, options);
        }

        public void Reply(object message)
        {
            bus.Reply(message);
        }

        public void Reply<T>(Action<T> messageConstructor)
        {
            bus.Reply(messageConstructor);
        }

        public void SendLocal(object message, SendLocalOptions options)
        {
            bus.SendLocal(message, options);
        }

        public void SendLocal<T>(Action<T> messageConstructor, SendLocalOptions options)
        {
            bus.SendLocal(messageConstructor, options);
        }

        public void HandleCurrentMessageLater()
        {
            bus.HandleCurrentMessageLater();
        }

        public void ForwardCurrentMessageTo(string destination)
        {
            bus.ForwardCurrentMessageTo(destination);
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            bus.DoNotContinueDispatchingCurrentMessageToHandlers();
        }
    }
}