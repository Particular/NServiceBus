namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;

    class XmlSerialization : IDisposable
    {
        public XmlSerialization(Type messageType, Stream stream, object message, Conventions conventions, XmlSerializerCache cache, bool skipWrappingRawXml, string @namespace = DefaultNamespace)
        {
            this.messageType = messageType;
            this.message = message;
            this.conventions = conventions;
            this.cache = cache;
            this.skipWrappingRawXml = skipWrappingRawXml;
            this.@namespace = @namespace;
            writer = new RawXmlTextWriter(stream, new XmlWriterSettings
            {
                CloseOutput = false
            });
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void Serialize()
        {
            var doc = new XDocument(new XDeclaration("1.0", null, null));

            var elementName = messageType.SerializationFriendlyName();
            doc.Add(new XElement(elementName));
            WriteObject(doc.Root, elementName, messageType, message, true);

            SetDefaultNamespace(doc.Root, $"{@namespace}/{messageType.Namespace}");
            ForceEmptyTagsWithNewlines(doc);

            doc.WriteTo(writer);
            writer.Flush();
        }

        static void ForceEmptyTagsWithNewlines(XDocument document)
        {
            // this is to force compatibility with previous implementation,
            // in particular, to support nested objects with null properties in them.

            foreach (var childElement in 
                from x in document.DescendantNodes().OfType<XElement>()
                where x.IsEmpty && !x.HasAttributes
                select x)
            {
                childElement.Value = "\n";
            }
        }

        static void SetDefaultNamespace(XElement element, XNamespace newXmlns)
        {
            var currentXmlns = element.GetDefaultNamespace();
            if (currentXmlns == newXmlns)
            {
                return;
            }

            foreach (var descendant in element.DescendantsAndSelf()
                .Where(e => e.Name.Namespace == currentXmlns))
            {
                descendant.Name = newXmlns.GetName(descendant.Name.LocalName);
            }
        }

        List<string> GetBaseTypes()
        {
            var result = new List<string>();
            var baseType = messageType.BaseType;
            while (baseType != typeof(object) && baseType != null)
            {
                if (conventions.IsMessageType(baseType))
                {
                    if (!result.Contains(baseType.FullName))
                    {
                        result.Add(baseType.FullName);
                    }
                }

                baseType = baseType.BaseType;
            }

            foreach (var i in messageType.GetInterfaces())
            {
                if (conventions.IsMessageType(i))
                {
                    if (!result.Contains(i.FullName))
                    {
                        result.Add(i.FullName);
                    }
                }
            }

            return result;
        }

        void WriteObject(XElement elem, string name, Type type, object value, bool useNS = false)
        {
            if (type == typeof(object) && value.GetType().IsSimpleType())
            {
                var typeOfValue = value.GetType();
                var ns = (XNamespace) typeOfValue.Name;
                var prefix = typeOfValue.Name.ToLower();
                if (!elem.Attributes().Any(a => a.IsNamespaceDeclaration && a.Name.LocalName == prefix))
                {
                    elem.Add(new XAttribute(XNamespace.Xmlns + prefix, ns.NamespaceName));
                }

                elem.Add(new XElement(ns + name, value));

                return;
            }

            if (useNS)
            {
                var baseTypes = GetBaseTypes();
                WriteElementNamespaces(elem, baseTypes);
            }
            else
            {
                var xe = new XElement(name);
                elem.Add(xe);
                elem = xe;
            }

            Write(elem, type, value);
        }

        void Write(XElement elem, Type t, object obj)
        {
            if (obj == null)
            {
                // For null entries in a nullable array
                // See https://github.com/Particular/NServiceBus/issues/2706
                if (t.IsNullableType())
                {
                    elem.Value = "null";
                }

                return;
            }

            IEnumerable<PropertyInfo> properties;
            if (!cache.typeToProperties.TryGetValue(t, out properties))
            {
                cache.InitType(t);
                cache.typeToProperties.TryGetValue(t, out properties);
            }

            foreach (var prop in properties)
            {
                if (IsIndexedProperty(prop))
                {
                    throw new NotSupportedException($"Type {t.FullName} contains an indexed property named {prop.Name}. Indexed properties are not supported on message types.");
                }
                WriteEntry(elem, prop.Name, prop.PropertyType, DelegateFactory.CreateGet(prop).Invoke(obj));
            }

            foreach (var field in cache.typeToFields[t])
            {
                WriteEntry(elem, field.Name, field.FieldType, DelegateFactory.CreateGet(field).Invoke(obj));
            }
        }

        void WriteEntry(XElement elem, string name, Type type, object value)
        {
            if (value == null)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    return;
                }

                var args = type.GetGenericArguments();
                if (args.Length == 1 && args[0].IsValueType)
                {
                    var nullableType = typeof(Nullable<>).MakeGenericType(args);
                    if (type == nullableType)
                    {
                        elem.Add(new XElement(name, new XAttribute(xsiNamespace + "nil", true), null));

                        return;
                    }
                }

                return;
            }

            if (typeof(XContainer).IsAssignableFrom(type))
            {
                var container = (XContainer) value;
                if (skipWrappingRawXml)
                {
                    elem.Add(XElement.Parse(container.ToString()));
                }
                else
                {
                    elem.Add(new XElement(name, XElement.Parse(container.ToString())));
                }

                return;
            }

            if (type.IsValueType || type == typeof(string) || type == typeof(Uri) || type == typeof(char))
            {
                elem.Add(new XElement(name, value));
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                var xe = new XElement(name);

                if (type == typeof(byte[]))
                {
                    var base64String = Convert.ToBase64String((byte[]) value);
                    xe.Value = base64String;
                }
                else
                {
                    var baseType = typeof(object);

                    var interfaces = type.GetInterfaces();
                    if (type.IsInterface)
                    {
                        interfaces = interfaces.Union(new[]
                        {
                            type
                        }).ToArray();
                    }

                    //Get generic type from list: T for List<T>, KeyValuePair<T,K> for IDictionary<T,K>
                    foreach (var interfaceType in interfaces)
                    {
                        var arr = interfaceType.GetGenericArguments();
                        if (arr.Length != 1)
                        {
                            continue;
                        }

                        if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(type))
                        {
                            baseType = arr[0];
                            break;
                        }
                    }

                    foreach (var obj in (IEnumerable) value)
                    {
                        if (obj != null && obj.GetType().IsSimpleType())
                        {
                            WriteEntry(xe, obj.GetType().Name, obj.GetType(), obj);
                        }
                        else
                        {
                            WriteObject(xe, baseType.SerializationFriendlyName(), baseType, obj);
                        }
                    }
                }

                elem.Add(xe);
                return;
            }

            WriteObject(elem, name, type, value);
        }

        static bool IsIndexedProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo?.GetIndexParameters().Length > 0;
        }

        void WriteElementNamespaces(XElement elem, IReadOnlyList<string> baseTypes)
        {
            elem.Add(new XAttribute(XNamespace.Xmlns + "xsi", xsiNamespace),
                new XAttribute(XNamespace.Xmlns + "xsd", xsdNamespace));

            for (var i = 0; i < baseTypes.Count; i++)
            {
                var prefix = BaseType;
                if (i != 0)
                {
                    prefix += i;
                }

                elem.Add(new XAttribute(XNamespace.Xmlns + prefix, baseTypes[i]));
            }
        }

        XmlSerializerCache cache;
        Conventions conventions;
        object message;

        Type messageType;
        string @namespace;
        bool skipWrappingRawXml;
        XmlWriter writer;
        const string BaseType = "baseType";

        const string DefaultNamespace = "http://tempuri.net";
        static XNamespace xsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        static XNamespace xsdNamespace = "http://www.w3.org/2001/XMLSchema";

        class RawXmlTextWriter : XmlTextWriter
        {
            public RawXmlTextWriter(Stream w, XmlWriterSettings settings) : base(w, null /*writes UTF-8 and omits the 'encoding' attribute in XML declaration*/)
            {
                this.settings = settings;
            }

            public override void WriteEndElement()
            {
                WriteFullEndElement();
            }

            public override void Close()
            {
                if (settings.CloseOutput)
                {
                    base.Close();
                }
            }

            readonly XmlWriterSettings settings;
        }
    }
}