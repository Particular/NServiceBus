using NServiceBus.Unicast.Config;

namespace NServiceBus.Host
{
    public class Order
    {
        internal ConfigUnicastBus config;

        public void Specify<TFirst>()
        {
            config.LoadMessageHandlers<TFirst>();
        }

        public void Specify<T>(First<T> ordering)
        {
            config.LoadMessageHandlers(ordering);
        }
    }
}
