#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Marks an API as not supported in a specific environment.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class NotSupportedInEnvironmentAttribute : Attribute
{
    /// <summary>
    /// Marks the target as not supported in the specified environment.
    /// </summary>
    /// <param name="environmentId">The environment identifier (e.g., AzureFunctionsIsolated).</param>
    /// <param name="reason">Explanation of why the API is not supported and what to use instead.</param>
    public NotSupportedInEnvironmentAttribute(string environmentId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        EnvironmentId = environmentId;
        Reason = reason;
    }

    /// <summary>
    /// The environment identifier where this API is not supported.
    /// </summary>
    public string EnvironmentId { get; }

    /// <summary>
    /// Explanation of why the API is not supported and what to use instead.
    /// </summary>
    public string Reason { get; }
}