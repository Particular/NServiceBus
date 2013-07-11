namespace NServiceBus
{
    using System;

    /// <summary>
    /// Static extension methods to Configure.
    /// </summary>
    public static class EndpointConventions
    {
        /// <summary>
        /// Sets the function that specified the name of this endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <param name="definesEndpointName"></param>
        public static Configure DefineEndpointName(this Configure config, Func<string> definesEndpointName)
        {
            Configure.GetEndpointNameAction = definesEndpointName;
            return config;
        }

        /// <summary>
        /// Sets the function that specified the name of this endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <param name="name"></param>
        public static Configure DefineEndpointName(this Configure config, string name)
        {
            Configure.GetEndpointNameAction = () => name;
            return config;
        }

    }
}
