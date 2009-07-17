using Messages;
using NServiceBus;
using NServiceBus.Host;

namespace Subscriber2
{
    /// <summary>
    /// Showing how to manage subscriptions manually
    /// </summary>
    class Subscriber2Endpoint : IMessageEndpoint
    {
        public IBus Bus { get; set; }

        public void OnStart()
        {
            Bus.Subscribe<IEvent>();
        }

        public void OnStop()
        {
            Bus.Unsubscribe<IEvent>();
        }
    }
}
