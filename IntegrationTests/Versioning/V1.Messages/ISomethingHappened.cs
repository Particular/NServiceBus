using NServiceBus;

namespace V1.Messages
{
    public interface ISomethingHappened : IEvent
    {
        int SomeData { get; set; }
    }
}
