namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;

    public interface IConsumeEvents : IDisposable
    {
        void Start();
        void Stop();
    }
}