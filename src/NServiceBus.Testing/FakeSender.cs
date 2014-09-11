namespace NServiceBus.Testing
{
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class FakeSender : ISendMessages
    {
        public void Send(TransportMessage message, SendOptions sendOptions)
        {

        }
    }
}