namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using System;
    using Apache.NMS;

    public interface IConsumeEvents : IDisposable
    {
        void Start(ISession session, IProcessMessages messageProcessor);
        void Stop();
    }
}