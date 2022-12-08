namespace NServiceBus.Recoverability;

using System;
using System.Collections.Generic;
using Extensibility;

/// <summary>
/// 
/// </summary>
public class RecoverabilityConfiguration
{
    /// <summary>
    /// 
    /// </summary>
    public int? MaximumImmediateRetries { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public int? MaximumDelayedRetries { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public TimeSpan? DelayedRetriesTimeIncrease { get; set; }
}

/// <summary>
/// 
/// </summary>
public interface IRecoverabilityConfiguration
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    RecoverabilityConfiguration OnError(Exception exception, object failedMessage,
        IReadOnlyDictionary<string, string> messageHeaders, ContextBag extensions);
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TConfiguration"></typeparam>
public interface IUseRecoverabilityConfiguration<TConfiguration> where TConfiguration : IRecoverabilityConfiguration, new()
{
}