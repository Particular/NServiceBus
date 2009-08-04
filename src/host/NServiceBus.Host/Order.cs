using NServiceBus.Unicast.Config;

namespace NServiceBus.Host
{
    /// <summary>
    /// Used to specify the order in which message handlers will be activated.
    /// </summary>
    public class Order
    {
        internal ConfigUnicastBus config;

        /// <summary>
        /// Specifies that the given type will be activated before all others.
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        public void Specify<TFirst>()
        {
            config.LoadMessageHandlers<TFirst>();
        }

        /// <summary>
        /// Specifies an ordering of multiple types using the syntax:
        /// First{H1}.Then{H2}().Then{H3}()... etc
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ordering"></param>
        public void Specify<T>(First<T> ordering)
        {
            config.LoadMessageHandlers(ordering);
        }
    }
}
