#pragma warning disable 1591
namespace NServiceBus
{
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", Replacement = "IConfigureLogging", RemoveInVersion = "6.0")]
    public interface IWantCustomLogging
    {
    }
}