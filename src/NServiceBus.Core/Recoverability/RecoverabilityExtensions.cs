namespace NServiceBus.Recoverability;

/// <summary>
/// 
/// </summary>
public static class RecoverabilityExtensions
{
    internal const string CustomImmediateConfigKey = "NServiceBus.Recoverability.Immediate";
    internal const string CustomDelayedConfigKey = "NServiceBus.Recoverability.Delayed";

    /// <summary>
    /// Configures a custom recoverability configuration for this pipeline invocation. This setting is applicable for the current message being processed and all exceptions thrown after calling this method.
    /// Use <code>null</code> as a parameter to reset any previous configuration and to apply the endpoint's recoverability configuration.
    /// </summary>
    public static void UseRecoverabilityConfiguration(this IMessageProcessingContext pipelineContext,
        ImmediateConfig immediateConfig, DelayedConfig delayedConfig)
    {
        pipelineContext.UseRecoverabilityConfiguration(immediateConfig);
        pipelineContext.UseRecoverabilityConfiguration(delayedConfig);
    }

    /// <summary>
    /// 
    /// </summary>
    public static void UseRecoverabilityConfiguration(this IMessageProcessingContext pipelineContext, ImmediateConfig immediateConfig)
    {
        //TODO log if overriding existing config
        pipelineContext.Extensions.SetOnRoot(CustomImmediateConfigKey, immediateConfig);
    }

    /// <summary>
    /// 
    /// </summary>
    public static void UseRecoverabilityConfiguration(this IMessageProcessingContext pipelineContext, DelayedConfig delayedConfig)
    {
        //TODO log if overriding existing config
        pipelineContext.Extensions.SetOnRoot(CustomDelayedConfigKey, delayedConfig);
    }
}