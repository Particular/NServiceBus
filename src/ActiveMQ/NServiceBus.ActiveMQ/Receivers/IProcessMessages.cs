﻿namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using System;

    using Apache.NMS;

    using NServiceBus.Unicast.Transport.Transactional;

    public interface IProcessMessages : IDisposable
    {
        Action<string, Exception> EndProcessMessage { get; set; }
        Func<TransportMessage, bool> TryProcessMessage { get; set; }

        void ProcessMessage(IMessage message);

        void Start(TransactionSettings transactionSettings);

        void Stop();

        IMessageConsumer CreateMessageConsumer(string destination);
    }
}