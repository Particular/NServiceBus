namespace NServiceBus.RabbitMq
{
    using System;
    using System.Linq;
    using Utils.Reflection;

    public class RabbitMqTopicBuilder
    {
        public static string GetRoutingKeyForPublish(Type eventType)
        {
            return GetRoutingKey(eventType);
        }

        public static string GetRoutingKeyForBinding(Type eventType)
        {
            if (eventType == typeof(IEvent) || eventType == typeof(object))
                return "#";


            return GetRoutingKey(eventType) + ".#";
        }

        static string GetRoutingKey(Type type, string key = "")
        {
            var baseType = type.BaseType;


            if (baseType != null && !baseType.IsSystemType())
                key = GetRoutingKey(baseType, key);


            var interfaces = type.GetInterfaces()
                                 .Where(i => !i.IsSystemType() && !i.IsNServiceBusMarkerInterface()).ToList();

            var implementedInterface = interfaces.FirstOrDefault();

            if (implementedInterface != null)
                key = GetRoutingKey(implementedInterface, key);


            if (!string.IsNullOrEmpty(key))
                key += ".";

            return key + type.FullName.Replace(".", "-");
        }
    }


}