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
        /// <param name="specifier"></param>
        /// <returns></returns>
        ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier);
    }
    /// <summary>
    /// Generic helper interface for IConfigureRole
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigureRole<T> : IConfigureRole where T : IRole
    {
    }
}