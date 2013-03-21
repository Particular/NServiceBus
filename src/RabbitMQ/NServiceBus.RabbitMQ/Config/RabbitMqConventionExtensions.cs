namespace NServiceBus
{
    using System;
    using Settings;
    using Transports.RabbitMQ.Config;

    /// <summary>
    /// Adds access to the RabbitMQ conventions to the global convention object
    /// </summary>
    public static class RabbitMqConventionExtensions
    {
        /// <summary>
        /// RabbitMq conventions.
        /// </summary>
        /// <param name="conventions"></param>
        /// <param name="action">A lambda to set the advance settings.</param>
        /// <returns></returns>
        public static Conventions RabbitMq(this Conventions conventions, Action<RabbitMqConventions> action)
        {
            action(new RabbitMqConventions());
            return conventions;
        }
    }
}