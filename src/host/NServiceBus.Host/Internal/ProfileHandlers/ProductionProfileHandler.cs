using System.Collections.Specialized;
using Common.Logging;
using NServiceBus.Utils.Reflection;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Production profile.
    /// </summary>
    public class ProductionProfileHandler : IHandleProfileConfiguration<Production>
    {
        private IConfigureThisEndpoint spec;

        void IHandleProfileConfiguration.Init(IConfigureThisEndpoint specifier)
        {
            spec = specifier;
        }

        void IHandleProfileConfiguration.ConfigureSagas(Configure busConfiguration)
        {
            if (!(spec is ISpecify.MyOwn.SagaPersistence))
                busConfiguration.NHibernateSagaPersister();
        }

        void IHandleProfileConfiguration.ConfigureSubscriptionStorage(Configure busConfiguration)
        {
            if (spec is ISpecify.MyOwn.SubscriptionStorage)
                return;

            var storageType = spec.GetType().GetGenericallyContainedType(typeof (ISpecify.ToUse.SubscriptionStorage<>),
                                                                         typeof (ISubscriptionStorage));

            if (storageType != null)
                Configure.TypeConfigurer.ConfigureComponent(storageType, ComponentCallModelEnum.Singleton);
            else
                busConfiguration.DBSubcriptionStorage();
        }
    }
}
