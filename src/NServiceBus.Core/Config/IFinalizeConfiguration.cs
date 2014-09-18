#pragma warning disable 1591
namespace NServiceBus.Config
{
     [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "`IFinalizeConfiguration` is no longer in use. Please use the Feature concept instead")]
    public interface IFinalizeConfiguration
    {
        void FinalizeConfiguration(Configure config);
    }
}