#pragma warning disable 1591
namespace NServiceBus
{
    [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Replacement = "INeedInitialization and IConfigureThisEndpoint")]
    public interface IWantCustomInitialization
    {
        void Init();
    }
}
