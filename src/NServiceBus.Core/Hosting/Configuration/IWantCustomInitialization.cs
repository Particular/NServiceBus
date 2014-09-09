#pragma warning disable 1591
namespace NServiceBus
{
    [ObsoleteEx(
        RemoveInVersion = "6", 
        TreatAsErrorFromVersion = "5", 
        Message = "Please use `INeedInitialization` or `IConfigureThisEndpoint`")]
    public interface IWantCustomInitialization
    {
        void Init();
    }
}
