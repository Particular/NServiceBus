namespace NServiceBus.Unicast
{
    using System;
    using System.Threading.Tasks;
    using Janitor;
    using Pipeline;

    [SkipWeaving]
    internal partial class ContextualBus : IBus, IContextualBus
    {
        public ContextualBus(BehaviorContextStacker contextStacker, StaticBus bus)
        {
            this.contextStacker = contextStacker;
            this.bus = bus;
        }

        public Task SendAsync<T>(Action<T> messageConstructor, NServiceBus.SendOptions options)
        {
            return bus.SendAsync(messageConstructor, options, incomingContext);
        }

        public Task SendAsync(object message, NServiceBus.SendOptions options)
        {
            return bus.SendAsync(message, options, incomingContext);
        }

        public IBusContext CreateSendContext()
        {
            return new BusContext(incomingContext);
        }

        [Obsolete("", true)]
        public IMessageContext CurrentMessageContext
        {
            get { throw new NotImplementedException(); }
        }

        BehaviorContext incomingContext => contextStacker.GetCurrentOrRootContext();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected
        }

        BehaviorContextStacker contextStacker;
        StaticBus bus;
    }
}