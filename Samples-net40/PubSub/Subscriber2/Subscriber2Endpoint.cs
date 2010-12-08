using MyMessages;
using NServiceBus;
using NServiceBus.Host;

namespace Subscriber2
{
    /// <summary>
    /// Showing how to manage subscriptions manually
    /// </summary>
    class Subscriber2Endpoint : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Bus.Subscribe<IEvent>();
        }

        public void Stop()
        {
            Bus.Unsubscribe<IEvent>();
        }
    }
}
