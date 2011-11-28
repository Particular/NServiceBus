namespace NServiceBus.Timeout.Hosting.Azure
{
    using Core;

    public interface IDetermineWhoCanSend
    {
        bool CanSend(TimeoutData data);
    }
}