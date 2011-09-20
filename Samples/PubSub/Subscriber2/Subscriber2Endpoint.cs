using NServiceBus;

namespace Subscriber2
{
    using MyMessages;

    /// <summary>
    /// Showing how to manage subscriptions manually
    /// </summary>
    class Subscriber2Endpoint : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Bus.Subscribe<IMyEvent>();
        }

        public void Stop()
        {
            Bus.Unsubscribe<IMyEvent>();
        }
    }
}
