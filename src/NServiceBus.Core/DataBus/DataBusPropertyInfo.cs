namespace NServiceBus
{
    using System;

    class DataBusPropertyInfo
    {
        public Func<object, object> Getter;
        public string Name;
        public Action<object, object> Setter;
    }
}