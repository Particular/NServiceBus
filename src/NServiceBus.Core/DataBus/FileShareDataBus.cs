namespace NServiceBus
{
    using System;
    using NServiceBus.DataBus;

    /// <summary>
    /// Base class for data bus definitions
    /// </summary>
    public class FileShareDataBus : DataBusDefinition
    {
        protected internal override Type ProvidedByFeature()
        {
            return typeof(Features.DataBus);
        }
    }
}