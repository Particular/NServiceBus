namespace NServiceBus
{
    using System;
    using Transports.RabbitMQ.Config;

    /// <summary>
    /// Adds access to the RabbitMQ conventions to the global convention object
    /// </summary>
    public static class RabbitMqConventionExtensions
    {
        /// <summary>
        /// Adds the RabbitMq wmethod to the global object
        /// </summary>
        /// <param name="conventions"></param>
        /// <param name="userConventions"></param>
        /// <returns></returns>
        public static Conventions RabbitMq(this Conventions conventions,Action<RabbitMqConventions> userConventions)
        {
            userConventions(new RabbitMqConventions());

            return conventions;
        }
    }
}