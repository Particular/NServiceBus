using NServiceBus;

namespace V1.Messages
{
    public interface SomethingHappened : IMessage
    {
        int SomeData { get; set; }
    }
}
