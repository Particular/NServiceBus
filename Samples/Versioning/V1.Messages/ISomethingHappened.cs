using NServiceBus;

namespace V1.Messages
{
    public interface ISomethingHappened : IMessage
    {
        int SomeData { get; set; }
    }
}
