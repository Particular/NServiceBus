namespace NServiceBus.Hosting.Roles
{
    using Unicast.Config;

    /// <summary>
    /// Interface that enables configuration based on specified role
    /// </summary>
    public interface IConfigureRole
    {
        /// <summary>
        /// Applies the role configuration
        /// </summary>
        ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier, Configure configure);
    }
    /// <summary>
    /// Generic helper interface for IConfigureRole
    /// </summary>
    public interface IConfigureRole<T> : IConfigureRole where T : IRole
    {
    }
}