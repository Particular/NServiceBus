using System;
using System.Collections.Generic;
using System.Reflection;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    public class ComplexType
    {
        private readonly IList<Element> elements = new List<Element>();
        private string name;
        private string baseName;

        private ComplexType()
        {
        }

        public string Name
        {
            get { return name; }
        }

        public string BaseName
        {
            get { return baseName; }
        }

        public IEnumerable<Element> Elements
        {
            get { return elements; }
        }

        public static ComplexType Scan(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(object) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(TimeSpan) || type.IsEnum)
                return null;

            ComplexType complex = new ComplexType();
            complex.name = Reflect.GetTypeNameFrom(type);

            if (Repository.IsNormalizedList(type))
            {
                Type enumerated = Reflect.GetEnumeratedTypeFrom(type);

                Element e = Element.Scan(enumerated, enumerated.Name);

                if (e != null)
                {
                    e.UnboundMaxOccurs();
                    e.MakeNillable();

                    complex.elements.Add(e);
                }
            }
            else
            {
                Type baseType = null;

                if (!type.IsInterface)
                    if (type.BaseType != typeof (object) && type.BaseType != null)
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
                    complex.baseName = baseType.Name;
                    propsToIgnore = new List<PropertyInfo>(baseType.GetProperties());
                }

                foreach (PropertyInfo prop in type.GetProperties())
                {
                    if (IsInList(prop, propsToIgnore))
                        continue;

                    if (!prop.CanRead || !prop.CanWrite)
                        continue;

                    Repository.Handle(prop.PropertyType);

                    Element e = Element.Scan(prop.PropertyType, prop.Name);

                    if (e != null)
                        complex.elements.Add(e);
                }
            }

            return complex;
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
