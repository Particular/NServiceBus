namespace NServiceBus.Azure
{
    using Timeout.Core;

    public interface IDetermineWhoCanSend
    {
        bool CanSend(TimeoutData data);
    }
}