namespace NServiceBus.Config
{
    /// <summary>
    /// Implementers will be called after NServiceBus.Configure.With completes and a container
    /// has been set. 
    /// </summary>
    [ObsoleteEx(Replacement = "NServiceBus!NServiceBus.INeedInitialization", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
    public interface INeedInitialization
    {
        /// <summary>
        /// Implementers will include custom initialization code here.
        /// </summary>
        void Init();
    }
}
