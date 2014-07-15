namespace NServiceBus.Hosting.Roles.Handlers
{
    using System.Linq;
    using Persistence;

    /// <summary>
    /// Configuring the right persistence based on  <see cref="UsingPersistence{T}"/> role on the endpoint config
    /// </summary>
    class PersistenceRoleHandler : IConfigureRole<UsingPersistence<PersistenceDefinition>>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specifier"></param>
        /// <param name="config"></param>
        public void ConfigureRole(IConfigureThisEndpoint specifier, Configure config)
        {
            var persistenceDefinitionType =
                specifier.GetType()
                    .GetInterfaces()
                    .SelectMany(i => i.GetGenericArguments())
                    .Single(t => typeof(PersistenceDefinition).IsAssignableFrom(t));

            config.UsePersistence(persistenceDefinitionType);
        }
    }
}