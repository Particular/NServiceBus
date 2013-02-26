namespace NServiceBus.Transports.ActiveMQ
{
    public interface IMessageCounter
    {
        void Increment();

        void Decrement();

        bool Wait(int timeout);
    }
}