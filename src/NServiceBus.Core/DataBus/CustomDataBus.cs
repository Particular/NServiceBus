namespace NServiceBus
{
    using System;
    using DataBus;
    using Features;

    class CustomDataBus : DataBusDefinition
    {
        public CustomDataBus(Type dataBusType)
        {
            DataBusType = dataBusType;
        }

        protected internal override Type ProvidedByFeature()
        {
            return typeof(CustomIDataBus);
        }

        public Type DataBusType { get; }
    }
}