namespace NServiceBus.Features
{
    using System;
    using NServiceBus.DataBus;

    class CustomIDataBus : Feature, IProvideService<DataBusStorage>
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var customStorageType = context.Settings.Get<Type>("CustomDataBusType");

            var customStorage = (IDataBus)Activator.CreateInstance(customStorageType);

            context.RegisterService(new DataBusStorage(customStorage));
        }
    }
}