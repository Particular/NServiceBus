namespace NServiceBus.Faults
{
    /// <summary>
    /// Interface for defining how message failures will be handled.
    /// </summary>
    [ObsoleteEx(
       Message = "IManageMessageFailures is no longer an extension point. If you want full control over what happens when a message fails (including retries) please override the MoveFaultsToErrorQueue behavior. If you just want to get notified when messages are being moved please use BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e=>{}) ",
       RemoveInVersion = "7",
       TreatAsErrorFromVersion = "6")]
    public interface IManageMessageFailures
    {
    }
}
