namespace NServiceBus.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Support;

    /// <summary>
    /// Provides information about the process hosting this endpoint.
    /// </summary>
    public class HostInformation
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="hostId">The id of the host.</param>
        /// <param name="displayName">The display name of the host.</param>
        public HostInformation(Guid hostId, string displayName)
            : this(hostId, displayName, new Dictionary<string, string>
            {
                {"Machine", RuntimeEnvironment.MachineName},
                {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                {"UserName", Environment.UserName}
            })
        {
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="hostId">The id of the host.</param>
        /// <param name="displayName">The display name of the host.</param>
        /// <param name="properties">A set of properties for the host. This might vary from host to host.</param>
        public HostInformation(Guid hostId, string displayName, Dictionary<string, string> properties)
        {
            HostId = hostId;
            DisplayName = displayName;
            Properties = properties;
        }

        /// <summary>
        /// The unique identifier for this host.
        /// </summary>
        public Guid HostId { get; private set; }

        /// <summary>
        /// The display name of this host.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// A set of properties for the host. This might vary from host to host.
        /// </summary>
        public Dictionary<string, string> Properties { get; private set; }
    }
}