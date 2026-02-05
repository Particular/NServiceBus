#nullable enable

namespace NServiceBus;

/// <summary>
/// Contains environment identifiers used by NServiceBus to identify specific runtime environments.
/// </summary>
static class EnvironmentIds
{
    /// <summary>
    /// Serverless environment.
    /// </summary>
    internal const string Serverless = nameof(Serverless);
}