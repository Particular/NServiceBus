namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Indicates that this class contains logic that needs to run just before
    /// configuration is finalized
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IWantToRunBeforeConfigurationIsFinalized
    {
        /// <summary>
        /// Invoked before configuration is finalized and locked
        /// </summary>
        void Run(Configure config);
    }
}