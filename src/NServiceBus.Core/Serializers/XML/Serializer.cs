namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using NServiceBus.Utils.Reflection;

    class RawXmlTextWriter : XmlTextWriter
    {
        readonly XmlWriterSettings settings;

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
    }

    class Serializer : IDisposable
    {
        const string BaseType = "baseType";

        const string DefaultNamespace = "http://tempuri.net";

        Type messageType;
        XmlWriter writer;
        object message;
        Conventions conventions;
        XmlSerializerCache cache;
        bool skipWrappingRawXml;
        string @namespace;
        
        public Serializer(Type messageType, Stream stream, object message, Conventions conventions, XmlSerializerCache cache, bool skipWrappingRawXml, string @namespace = DefaultNamespace)
        {
            this.messageType = messageType;
            this.message = message;
            this.conventions = conventions;
            this.cache = cache;
            this.skipWrappingRawXml = skipWrappingRawXml;
            this.@namespace = @namespace;
            writer = new RawXmlTextWriter(stream, new XmlWriterSettings { CloseOutput = false });
        }

        public void Serialize()
        {
            var doc = new XDocument(new XDeclaration("1.0", null, null));
            
            var t = mapper.GetMappedTypeFor(message.GetType());

            var elementName = t.SerializationFriendlyName();
            doc.Add(new XElement(elementName));
            WriteObject(doc.Root, elementName, t, message, true);

            SetDefaultNamespace(doc.Root, string.Format("{0}/{1}", @namespace, GetNamespace(message)));
            ForceEmptyTagsWithNewlines(doc);

            doc.WriteTo(writer);
            writer.Flush();
        }

        private static void ForceEmptyTagsWithNewlines(XDocument document)
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
                return;

            foreach (var descendant in element.DescendantsAndSelf()
                .Where(e => e.Name.Namespace == currentXmlns))
            {
                descendant.Name = newXmlns.GetName(descendant.Name.LocalName);
            }
        }


        static string FormatAsString(object value)
        {
            if (value == null)
            {
                return "null";
            }
            if (value is bool)
            {
                return XmlConvert.ToString((bool)value);
            }
            if (value is byte)
            {
                return XmlConvert.ToString((byte)value);
            }
            if (value is char)
            {
                return Escape((char)value);
            }
            if (value is double)
            {
                return XmlConvert.ToString((double)value);
            }
            if (value is ulong)
            {
                return XmlConvert.ToString((ulong)value);
            }
            if (value is uint)
            {
                return XmlConvert.ToString((uint)value);
            }
            if (value is ushort)
            {
                return XmlConvert.ToString((ushort)value);
            }
            if (value is long)
            {
                return XmlConvert.ToString((long)value);
            }
            if (value is int)
            {
                return XmlConvert.ToString((int)value);
            }
            if (value is short)
            {
                return XmlConvert.ToString((short)value);
            }
            if (value is sbyte)
            {
                return XmlConvert.ToString((sbyte)value);
            }
            if (value is decimal)
            {
                return XmlConvert.ToString((decimal)value);
            }
            if (value is float)
            {
                return XmlConvert.ToString((float)value);
            }
            if (value is Guid)
            {
                return XmlConvert.ToString((Guid)value);
            }
            if (value is DateTime)
            {
                return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
            }
            if (value is DateTimeOffset)
            {
                return XmlConvert.ToString((DateTimeOffset)value);
            }
            if (value is TimeSpan)
            {
                return XmlConvert.ToString((TimeSpan)value);
            }
            if (value is string)
            {
                return (string) value;
            }

            return Escape(value.ToString());
        }

#pragma warning disable 652

        static string Escape(char c)
        {
            if (c == 0x9 || c == 0xA || c == 0xD
                || (0x20 <= c && c <= 0xD7FF)
                || (0xE000 <= c && c <= 0xFFFD)
                || (0x10000 <= c && c <= 0x10ffff)
                )
            {
                string ss = null;
                switch (c)
                {
                    case '<':
                        ss = "&lt;";
                        break;

                    case '>':
                        ss = "&gt;";
                        break;

                    case '"':
                        ss = "&quot;";
                        break;

                    case '\'':
                        ss = "&apos;";
                        break;

                    case '&':
                        ss = "&amp;";
                        break;
                }
                if (ss != null)
                {
                    return ss;
                }
            }
            else
            {
                return String.Format("<![CDATA[&#x{0:X}]]>;", (int)c);
            }

            //Should not get here but just in case!
            return c.ToString();
        }

        static string Escape(string stringToEscape)
        {


            if (string.IsNullOrEmpty(stringToEscape))
            {
                return stringToEscape;
            }

            StringBuilder builder = null; // initialize if we need it

            var startIndex = 0;
            for (var i = 0; i < stringToEscape.Length; ++i)
            {
                var c = stringToEscape[i];
                if (c == 0x9 || c == 0xA || c == 0xD
                    || (0x20 <= c && c <= 0xD7FF)
                    || (0xE000 <= c && c <= 0xFFFD)
                    || (0x10000 <= c && c <= 0x10ffff)
                    )
                {
                    string ss = null;
                    switch (c)
                    {
                        case '<':
                            ss = "&lt;";
                            break;

                        case '>':
                            ss = "&gt;";
                            break;

                        case '"':
                            ss = "&quot;";
                            break;

                        case '\'':
                            ss = "&apos;";
                            break;

                        case '&':
                            ss = "&amp;";
                            break;
                    }
                    if (ss != null)
                    {
                        if (builder == null)
                        {
                            builder = new StringBuilder(stringToEscape.Length + ss.Length);
                        }
                        if (startIndex < i)
                        {
                            builder.Append(stringToEscape, startIndex, i - startIndex);
                        }
                        startIndex = i + 1;
                        builder.Append(ss);
                    }
                }
                else
                {
                    // invalid characters
                    if (builder == null)
                    {
                        builder = new StringBuilder(stringToEscape.Length + 8);
                    }
                    if (startIndex < i)
                    {
                        builder.Append(stringToEscape, startIndex, i - startIndex);
                    }
                    startIndex = i + 1;
                    builder.AppendFormat("&#x{0:X};", (int)c);
                }
            }

            if (startIndex < stringToEscape.Length)
            {
                if (builder == null)
                {
                    return stringToEscape;
                }
                builder.Append(stringToEscape, startIndex, stringToEscape.Length - startIndex);
            }

            if (builder != null)
            {
                return builder.ToString();
            }

            //Should not get here but just in case!
            return stringToEscape;
        }
#pragma warning restore 652

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
                var ns = (XNamespace)typeOfValue.Name;
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
                var baseTypes = GetBaseTypes(value);
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
                    elem.Value = "null";

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
                        WriteEntry(elem, name, typeof(string), "null");
                        return;
                    }
                }

                return;
            }

            if (typeof(XContainer).IsAssignableFrom(type))
            {
                var container = (XContainer)value;
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
                    var base64String = Convert.ToBase64String((byte[])value);
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

                    foreach (var obj in ((IEnumerable)value))
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
            elem.Add(new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                     new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"));

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

        public void Dispose()
        {
            //Injected at compile time
        }
    }
}