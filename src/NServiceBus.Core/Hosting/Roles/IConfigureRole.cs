namespace NServiceBus.Hosting.Roles
{
    /// <summary>
    /// Interface that enables configuration based on specified role
    /// </summary>
    public interface IConfigureRole
    {
        /// <summary>
        /// Applies the role configuration
        /// </summary>
        void ConfigureRole(IConfigureThisEndpoint specifier, Configure config);
    }
    /// <summary>
    /// Generic helper interface for IConfigureRole
    /// </summary>
    public interface IConfigureRole<T> : IConfigureRole where T : IRole
    {
    }
}