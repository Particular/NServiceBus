namespace NServiceBus
{
    using System;
    using NServiceBus.DataBus;
    using NServiceBus.Features;

    class CustomDataBus : DataBusDefinition
    {
        protected internal override Type ProvidedByFeature()
        {
            return typeof(CustomIDataBus);
        }
    }
}