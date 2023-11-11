namespace NServiceBus;

using System;

/// <summary>
/// Meant for staging future obsoletes.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
sealed class PreObsoleteAttribute : Attribute
{
    public PreObsoleteAttribute(string contextUrl)
    {
        ContextUrl = contextUrl;
    }

    public string ContextUrl { get; }

    public string ReplacementTypeOrMember { get; set; }

    public string Message { get; set; }

    public string Note { get; set; }
}
