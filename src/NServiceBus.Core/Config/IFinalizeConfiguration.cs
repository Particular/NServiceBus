namespace NServiceBus.Config
{
    /// <summary>
    /// Interface used to finalize configuration. This is the final point where the container can be altered.
    /// </summary>
    /// 
     [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "5",
            Message = "IFinalizeConfiguration is no longer in use. Please use the Feature concept instead")]
    public interface IFinalizeConfiguration
    {
        /// <summary>
        /// Invoked by the framework when the configuration is to be finalized
        /// </summary>
        void FinalizeConfiguration(Configure config);
    }
}