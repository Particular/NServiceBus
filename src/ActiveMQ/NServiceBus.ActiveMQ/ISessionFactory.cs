namespace NServiceBus.Transport.ActiveMQ
{
    using Apache.NMS;

    public interface ISessionFactory
    {
        ISession GetSession();

        void Release(ISession session);

        void SetSessionForCurrentThread(ISession session);

        void RemoveSessionForCurrentThread();
    }
}