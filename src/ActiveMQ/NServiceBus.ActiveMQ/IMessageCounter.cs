namespace NServiceBus.Transport.ActiveMQ
{
    public interface IMessageCounter
    {
        void Increment();

        void Decrement();

        bool Wait(int timeout);
    }
}