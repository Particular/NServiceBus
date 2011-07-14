using System;
using System.Collections.Generic;
using System.Reflection;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    public class ComplexType
    {
        private readonly IList<Element> elements = new List<Element>();

        private ComplexType()
        {
            TimeToBeReceived = TimeSpan.Zero;
        }

        public string Name { get; private set; }

        public string BaseName { get; private set; }

        public TimeSpan TimeToBeReceived { get; private set; }

        public IEnumerable<Element> Elements
        {
            get { return elements; }
        }

        public static ComplexType Scan(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(object) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(DateTimeOffset) || type.IsEnum || type == typeof(Decimal))
                return null;

            ComplexType complex = new ComplexType();
            complex.Name = Reflect.GetTypeNameFrom(type);

            if (Repository.IsNormalizedList(type))
            {
                Type enumerated = Reflect.GetEnumeratedTypeFrom(type);

                Element e = Element.Scan(enumerated, Reflect.GetTypeNameFrom(enumerated));

                if (e != null)
                {
                    e.UnboundMaxOccurs();
                    e.MakeNillable();

                    complex.elements.Add(e);
                }

                Repository.Handle(enumerated);
            }
            else
            {
                Type baseType = null;

                if (!type.IsInterface)
                    if (type.BaseType != typeof (object) && type.BaseType != typeof(ValueType) && type.BaseType != null)
                        baseType = type.BaseType;

                if (type.IsInterface)
                    foreach(Type i in type.GetInterfaces())
                        if (i == typeof(IMessage))
                            continue;
                        else
                        {
                            baseType = i;
                            break;
                        }

                List<PropertyInfo> propsToIgnore = new List<PropertyInfo>();

                if (baseType != null)
                {
                    complex.BaseName = baseType.Name;
                    propsToIgnore = new List<PropertyInfo>(baseType.GetProperties());
                }

                foreach (PropertyInfo prop in type.GetProperties())
                {
                    if (IsInList(prop, propsToIgnore))
                        continue;

                    if (!IsKeyValuePair(type) && (!prop.CanRead || !prop.CanWrite))
                        continue;

                    Repository.Handle(prop.PropertyType);

                    Element e = Element.Scan(prop.PropertyType, prop.Name);

                    if (e != null)
                        complex.elements.Add(e);
                }
            }

            foreach(TimeToBeReceivedAttribute a in type.GetCustomAttributes(typeof(TimeToBeReceivedAttribute), true))
                complex.TimeToBeReceived = a.TimeToBeReceived;

            return complex;
        }

        private static bool IsKeyValuePair(Type t)
        {
            Type[] args = t.GetGenericArguments();
            if (args == null)
                return false;
            if (args.Length != 2)
                return false;
            return (typeof(KeyValuePair<,>).MakeGenericType(args) == t);
        }

        private static bool IsInList(PropertyInfo prop, ICollection<PropertyInfo> propsToIgnore)
        {
            if (propsToIgnore.Contains(prop))
                return true;
                                
            foreach(PropertyInfo pi in propsToIgnore)
                if (pi.Name == prop.Name)
                    return true;

            return false;
        }
    }
}
