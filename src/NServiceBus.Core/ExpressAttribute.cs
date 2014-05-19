namespace NServiceBus
{
    using System;

    /// <summary>
    /// Attribute to indicate that the message should not be written to disk.
    /// This will make the message vulnerable to server crashes or restarts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ExpressAttribute : Attribute
    {
    }
}