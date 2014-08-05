namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Hosting.Profiles;

    /// <summary>
    /// Implementors will receive the list of active Profiles from the <see cref="ProfileManager" />. 
    /// Implementors must implement <see cref="IHandleProfile"/>.
    /// </summary>
    public interface IWantTheListOfActiveProfiles
    {
        /// <summary>
        /// ActiveProfiles list will be set by the infrastructure.
        /// </summary>
        IEnumerable<Type> ActiveProfiles { get; set; }
    }
}