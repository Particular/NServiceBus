namespace NServiceBus
{
    using System;
    using NServiceBus.DataBus;

    class CustomDataBus : DataBusDefinition
    {
        protected internal override Type ProvidedByFeature()
        {
            return typeof(Features.CustomIDataBus);
        }
    }
}