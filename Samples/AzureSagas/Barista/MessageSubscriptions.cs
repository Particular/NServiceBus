using System;
using CashierContracts;
using NServiceBus;

namespace Barista
{
    public interface IMessageSubscriptions : IDisposable
    {
        IDisposable Subscribe();
        void Unsubscribe();
    }

    public class MessageSubscriptions : Disposable,
                                        IMessageSubscriptions
    {
        private readonly IBus _bus;

        private static readonly Type[] MessageTypes = new Type[]
        {
            typeof(PrepareOrderMessage),
            typeof(PaymentCompleteMessage)                                                      
        };
        
        public MessageSubscriptions(IBus bus)
        {
            _bus = bus;
        }

        public IDisposable Subscribe()
        {
            foreach(var messageType in MessageTypes)
                _bus.Subscribe(messageType);
        
            return this;
        }

        public void Unsubscribe()
        {
            foreach(var messageType in MessageTypes)
                _bus.Unsubscribe(messageType);
        }

        protected override void DisposeManagedResources()
        {
            Unsubscribe();
        }
    }
}
