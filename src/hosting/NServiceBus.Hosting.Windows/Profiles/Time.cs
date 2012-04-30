using System;

namespace NServiceBus
{
    /// <summary>
    /// Indicates that this node will have a Timeout manager
    /// </summary>
    [Obsolete("TTimeout Profile is obsolete as Timeout Manager is on by default for Server and Publisher roles.")]
    public interface Time : IProfile
    {
    }
}
