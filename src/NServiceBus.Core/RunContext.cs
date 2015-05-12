namespace NServiceBus
{
    using System;

    class RunContext : IRunContext
    {
        readonly IBus bus;

        public RunContext(IBus bus)
        {
            this.bus = bus;
        }

        public void Subscribe(Type messageType)
        {
            bus.Subscribe(messageType);
        }

        public void Subscribe<T>()
        {
            bus.Subscribe<T>();
        }

        public void Unsubscribe(Type messageType)
        {
            bus.Unsubscribe(messageType);
        }

        public void Unsubscribe<T>()
        {
            bus.Unsubscribe<T>();
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
    }
}