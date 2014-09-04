namespace NServiceBus
{
    using System;

    /// <summary>
    /// Configuration options common for all transports
    /// </summary>
    public class TransportConfiguration
    {
        // ReSharper disable UnusedParameter.Global

        /// <summary>
        /// Configures the transport to use the given string as the connection string
        /// </summary>
        [ObsoleteEx(Replacement = "Use configuration.UseTransport<T>().ConnectionString(connectionString), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]

        public void ConnectionString(string connectionString)

        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Configures the transport to use the connection string with the given name
        /// </summary>
        [ObsoleteEx(Replacement = "Use configuration.UseTransport<T>().ConnectionStringName(name), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public void ConnectionStringName(string name)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string
        /// </summary>
        [ObsoleteEx(Replacement = "Use configuration.UseTransport<T>().ConnectionString(connectionString), where configuration is an instance of type BusConfiguration", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public void ConnectionString(Func<string> connectionString)
        {
            throw new InvalidOperationException();
        }
        // ReSharper restore UnusedParameter.Global
    }
}