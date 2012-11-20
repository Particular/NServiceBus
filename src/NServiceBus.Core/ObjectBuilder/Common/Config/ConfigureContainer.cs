namespace NServiceBus.ObjectBuilder.Common.Config
{
    ///<summary>
    /// Extension methods to specify a custom container type and/or instance
    ///</summary>
    public static class ConfigureContainer
    {
        ///<summary>
        /// Provide a custom IContainer type for use by NServiceBus
        ///</summary>
        ///<param name="configure">Configuration instance</param>
        ///<typeparam name="T">IContainer type</typeparam>
        ///<returns></returns>
        public static Configure UsingContainer<T>(this Configure configure) where T : class, IContainer, new()
        {
            UsingContainer(configure, new T());

            return configure;
        }
        
        ///<summary>
        /// Provide a custom IContainer instance for use by NServiceBus
        ///</summary>
        ///<param name="configure">Configuration instance</param>
        ///<param name="container">IContainer instance</param>
        ///<typeparam name="T">IContainer type</typeparam>
        ///<returns></returns>
        public static Configure UsingContainer<T>(this Configure configure, T container) where T : IContainer
        {
            ConfigureCommon.With(configure, container);

            return configure;
        }
    }
}