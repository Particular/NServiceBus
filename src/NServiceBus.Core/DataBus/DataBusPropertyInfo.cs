namespace NServiceBus
{
    using System;

    class DataBusPropertyInfo
    {
        public string Name;
        public Func<object, object> Getter;
        public Action<object, object> Setter;
    }
}