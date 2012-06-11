using NServiceBus;

namespace Subscriber2
{
    using MyMessages;

    /// <summary>
    /// Showing how to manage subscriptions manually
    /// </summary>
    class Subscriber2Endpoint : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            Bus.Subscribe<IMyEvent>();
        }

        public void Stop()
        {
            Bus.Unsubscribe<IMyEvent>();
        }
    }
}
