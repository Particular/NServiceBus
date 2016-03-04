namespace NServiceBus
{
    using System;
    using DataBus;
    using Features;

    class CustomDataBus : DataBusDefinition
    {
        protected internal override Type ProvidedByFeature()
        {
            return typeof(CustomIDataBus);
        }
    }
}