﻿namespace NServiceBus
{
    using System;

    /// <summary>
    /// Used to specify the name of the current endpoint.
    /// Will be used as the name of the input queue as well.
    /// </summary>
    [ObsoleteEx(Replacement = "NServiceBus.Hosting.EndpointNameAttribute in the NServiceBus.Host assembly", TreatAsErrorFromVersion = "5.0")]
    public sealed class EndpointNameAttribute : Attribute
    {
        /// <summary>
        /// Used to specify the name of the current endpoint.
        /// Will be used as the name of the input queue as well.
        /// </summary>
        public EndpointNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of the endpoint.
        /// </summary>
        public string Name { get; set; }
    }
}
