namespace NServiceBus;

using System;
using DataBus;
using Features;

class CustomDataBus : DataBusDefinition
{
    public CustomDataBus(Func<IServiceProvider, IDataBus> dataBusFactory)
    {
        DataBusFactory = dataBusFactory;
    }

    protected internal override Type ProvidedByFeature()
    {
        return typeof(CustomIDataBus);
    }

    public Func<IServiceProvider, IDataBus> DataBusFactory { get; }
}