using Timeout.MessageHandlers;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public interface IDetermineWhoCanSend
    {
        bool CanSend(TimeoutData data);
    }
}