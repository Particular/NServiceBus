using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;
using NServiceBus.UnitOfWork;

namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    /// <summary>
    /// Handles configuration related to the server role
    /// </summary>
    public class ServerRoleHandler : IConfigureRole<AsA_Server>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a server
        /// </summary>
        /// <param name="specifier"></param>
        /// <returns></returns>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            //todo raven unit of work
            //if (!Configure.Instance.Configurer.HasComponent<IManageUnitsOfWork>())
            //    Configure.Instance.NHibernateUnitOfWork();

            return Configure.Instance
                .Sagas()
                .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
                .UseDistributor()
                .UnicastBus()
                .ImpersonateSender(true);
        }
    }
}