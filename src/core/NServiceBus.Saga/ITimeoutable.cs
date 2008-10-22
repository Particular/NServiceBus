

namespace NServiceBus.Saga
{
    public interface ITimeoutable
    {
        void Timeout(object state);
    }
}
