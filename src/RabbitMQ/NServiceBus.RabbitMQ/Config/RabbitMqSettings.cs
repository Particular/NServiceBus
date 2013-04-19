namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using NServiceBus.Config;
    using Settings;
    using RabbitMQ;
    using Routing;

    /// <summary>
    /// The custom settings available for the RabbitMq transport
    /// </summary>
    public class RabbitMqSettings : ISetDefaultSettings
    {

        /// <summary>
        /// Setup the defaults
        /// </summary>
        public RabbitMqSettings()
        {
            InfrastructureServices.SetDefaultFor<IRoutingTopology>(typeof(ConventionalRoutingTopology),DependencyLifecycle.SingleInstance);
        }

        /// <summary>
        /// Use the direct routing topology with the given conventions
        /// </summary>
        /// <param name="routingKeyConvention"></param>
        /// <param name="exchangeNameConvention"></param>
        /// <returns></returns>
        public RabbitMqSettings UseDirectRoutingTopology(Func<Type, string> routingKeyConvention = null, Func<Address, Type, string> exchangeNameConvention = null)
        {
            if (routingKeyConvention == null)
            {
                routingKeyConvention = DefaultRoutingKeyConvention.GenerateRoutingKey;
            }

            if (exchangeNameConvention == null)
            {
                exchangeNameConvention = (address, eventType) => "amq.topic";   
            }

            InfrastructureServices.RegisterServiceFor<IRoutingTopology>(() =>
                {
                    var router = new DirectRoutingTopology
                        {
                            ExchangeNameConvention = exchangeNameConvention,
                            RoutingKeyConvention = routingKeyConvention
                        };

                    Configure.Instance.Configurer.RegisterSingleton<IRoutingTopology>(router);
                });
            return this;
        }

        /// <summary>
        /// Register a custom routing topology
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public RabbitMqSettings UseRoutingTopology<T>()
        {
            InfrastructureServices.RegisterServiceFor<IRoutingTopology>(typeof(T), DependencyLifecycle.SingleInstance);
            return this;
        }
    }
}