namespace NServiceBus.Transports.RabbitMQ.Config
{
    public interface IConnectionStringParser
    {
        IConnectionConfiguration Parse(string connectionString);
    }
}