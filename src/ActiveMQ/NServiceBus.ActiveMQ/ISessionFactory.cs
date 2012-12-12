namespace NServiceBus.ActiveMQ
{
    using Apache.NMS;

    public interface ISessionFactory
    {
        INetTxSession CreateSession();
    }
}