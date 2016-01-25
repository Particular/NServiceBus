namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Janitor;

    [SkipWeaving]
    class AutoDispatchBusSession : IBusSession
    {
        IBusSession session;

        public AutoDispatchBusSession(IBusSession session)
        {
            this.session = session;
        }

        public void Dispose()
        {
            session.Dispose();
        }

        public Task Send(object message, SendOptions options)
        {
            options.RequireImmediateDispatch();
            return session.Send(message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            options.RequireImmediateDispatch();
            return session.Send(messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            options.RequireImmediateDispatch();
            return session.Publish(message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions options)
        {
            options.RequireImmediateDispatch();
            return session.Publish(messageConstructor, options);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            options.RequireImmediateDispatch();
            return session.Subscribe(eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            options.RequireImmediateDispatch();
            return session.Unsubscribe(eventType, options);
        }

        public Task Dispatch()
        {
            return session.Dispatch();
        }
    }
}