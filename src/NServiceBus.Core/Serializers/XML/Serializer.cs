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
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Utils.Reflection;

    class Serializer:IDisposable
    {
        const string BaseType = "baseType";

        const string DefaultNamespace = "http://tempuri.net";

        List<Type> namespacesToAdd = new List<Type>();
        IMessageMapper mapper;
        StreamWriter writer;
        object message;
        Conventions conventions;
        XmlSerializerCache cache;
        bool skipWrappingRawXml;
        string @namespace;

        public Serializer(IMessageMapper mapper, Stream stream, object message, Conventions conventions, XmlSerializerCache cache, bool skipWrappingRawXml, string @namespace = DefaultNamespace)
        {
            this.mapper = mapper;
            writer = new StreamWriter(stream, Encoding.UTF8, 1024,true);
            this.message = message;
            this.conventions = conventions;
            this.cache = cache;
            this.skipWrappingRawXml = skipWrappingRawXml;
            this.@namespace = @namespace;
        }

        public void Serialize()
        {
            writer.WriteLine("<?xml version=\"1.0\" ?>");
            var t = mapper.GetMappedTypeFor(message.GetType());

            WriteObject(t.SerializationFriendlyName(), t, message, true);
            writer.Flush();
        }

        string GetNamespace(object target)
        {
            return mapper.GetMappedTypeFor(target.GetType()).Namespace;
        }

        static string FormatAsString(object value)
        {
            if (value is bool)
            {
                return XmlConvert.ToString((bool) value);
            }
            if (value is byte)
            {
                return XmlConvert.ToString((byte) value);
            }
            if (value is char)
            {
                return Escape((char) value);
            }
            if (value is double)
            {
                return XmlConvert.ToString((double) value);
            }
            if (value is ulong)
            {
                return XmlConvert.ToString((ulong) value);
            }
            if (value is uint)
            {
                return XmlConvert.ToString((uint) value);
            }
            if (value is ushort)
            {
                return XmlConvert.ToString((ushort) value);
            }
            if (value is long)
            {
                return XmlConvert.ToString((long) value);
            }
            if (value is int)
            {
                return XmlConvert.ToString((int) value);
            }
            if (value is short)
            {
                return XmlConvert.ToString((short) value);
            }
            if (value is sbyte)
            {
                return XmlConvert.ToString((sbyte) value);
            }
            if (value is decimal)
            {
                return XmlConvert.ToString((decimal) value);
            }
            if (value is float)
            {
                return XmlConvert.ToString((float) value);
            }
            if (value is Guid)
            {
                return XmlConvert.ToString((Guid) value);
            }
            if (value is DateTime)
            {
                return XmlConvert.ToString((DateTime) value, XmlDateTimeSerializationMode.RoundtripKind);
            }
            if (value is DateTimeOffset)
            {
                return XmlConvert.ToString((DateTimeOffset) value);
            }
            if (value is TimeSpan)
            {
                return XmlConvert.ToString((TimeSpan) value);
            }
            if (value is string)
            {
                return Escape((string) value);
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
                return String.Format("&#x{0:X};", (int) c);
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
                    builder.AppendFormat("&#x{0:X};", (int) c);
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

        List<string> GetBaseTypes(object message)
        {
            var result = new List<string>();

            var t = mapper.GetMappedTypeFor(message.GetType());

            var baseType = t.BaseType;
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

            foreach (var i in t.GetInterfaces())
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

        void WriteObject(string name, Type type, object value, bool useNS = false)
        {
            var element = name;

            if (type == typeof(object) && (value.GetType().IsSimpleType()))
            {
                if (!namespacesToAdd.Contains(value.GetType()))
                {
                    namespacesToAdd.Add(value.GetType());
                }

                writer.Write("<{0}>{1}</{0}>\n", value.GetType().Name.ToLower() + ":" + name, FormatAsString(value));

                return;
            }


            if (useNS)
            {
                var messageNamespace = GetNamespace(value);
                var baseTypes = GetBaseTypes(value);
                CreateStartElementWithNamespaces(messageNamespace, baseTypes, element);
            }
            else
            {
                writer.WriteLine("<{0}>", element);
            }

            Write(type, value);

            writer.WriteLine("</{0}>", element);
        }

        void Write(Type t, object obj)
        {
            if (obj == null)
            {
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
                    throw new NotSupportedException(string.Format("Type {0} contains an indexed property named {1}. Indexed properties are not supported on message types.", t.FullName, prop.Name));
                }
                WriteEntry(prop.Name, prop.PropertyType, DelegateFactory.CreateGet(prop).Invoke(obj));
            }

            foreach (var field in cache.typeToFields[t])
            {
                WriteEntry(field.Name, field.FieldType, DelegateFactory.CreateGet(field).Invoke(obj));
            }
        }

        void WriteEntry(string name, Type type, object value)
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
                        WriteEntry(name, typeof(string), "null");
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
                    writer.WriteLine("{0}", container);
                }
                else
                {
                    writer.WriteLine("<{0}>{1}</{0}>", name, container);
                }

                return;
            }

            if (type.IsValueType || type == typeof(string) || type == typeof(Uri) || type == typeof(char))
            {
                writer.WriteLine("<{0}>{1}</{0}>", name, FormatAsString(value));
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                writer.WriteLine("<{0}>", name);

                if (type == typeof(byte[]))
                {
                    var base64String = Convert.ToBase64String((byte[]) value);
                    writer.Write(base64String);
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


                    foreach (var obj in ((IEnumerable) value))
                    {
                        if (obj != null && obj.GetType().IsSimpleType())
                        {
                            WriteEntry(obj.GetType().Name, obj.GetType(), obj);
                        }
                        else
                        {
                            WriteObject(baseType.SerializationFriendlyName(), baseType, obj);
                        }
                    }
                }

                writer.WriteLine("</{0}>", name);
                return;
            }

            WriteObject(name, type, value);
        }

        static bool IsIndexedProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo != null)
            {
                return propertyInfo.GetIndexParameters().Length > 0;
            }

            return false;
        }

        void CreateStartElementWithNamespaces(string messageNamespace, List<string> baseTypes, string element)
        {
            writer.Write(
                "<{0} xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"",
                element);

            writer.Write(" xmlns=\"{0}/{1}\"", @namespace, messageNamespace);

            foreach (var t in namespacesToAdd)
            {
                writer.Write(" xmlns:{0}=\"{1}\"", t.Name.ToLower(), t.Name);
            }

            for (var i = 0; i < baseTypes.Count; i++)
            {
                var prefix = BaseType;
                if (i != 0)
                {
                    prefix += i;
                }

                writer.Write(" xmlns:{0}=\"{1}\"", prefix, baseTypes[i]);
            }

            writer.WriteLine(">");
        }

        public void Dispose()
        {
            //Injected at compile time   
        }
    }
}
