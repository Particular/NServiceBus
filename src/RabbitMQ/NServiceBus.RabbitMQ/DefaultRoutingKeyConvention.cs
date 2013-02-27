namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using System.Linq;
    using Utils.Reflection;

    public class DefaultRoutingKeyConvention
    {
        public static string GenerateRoutingKey(Type eventType)
        {
            return GetRoutingKey(eventType);
        }

        static string GetRoutingKey(Type type, string key = "")
        {
            var baseType = type.BaseType;


            if (baseType != null && !baseType.IsSystemType())
                key = GetRoutingKey(baseType, key);


            var interfaces = type.GetInterfaces()
                                 .Where(i => !ExtensionMethods.IsSystemType(i) && !ExtensionMethods.IsNServiceBusMarkerInterface(i)).ToList();

            var implementedInterface = interfaces.FirstOrDefault();

            if (implementedInterface != null)
                key = GetRoutingKey(implementedInterface, key);


            if (!string.IsNullOrEmpty(key))
                key += ".";

            return key + type.FullName.Replace(".", "-");
        }
    }
}