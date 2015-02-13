namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Utils.Reflection;

    class Serializer
    {
        const string BaseType = "baseType";

        const string DefaultNamespace = "http://tempuri.net";

        readonly List<Type> namespacesToAdd = new List<Type>();
        readonly IMessageMapper mapper;
        readonly Conventions conventions;
        readonly XmlSerializerCache cache;
        readonly bool skipWrappingRawXml;
        readonly string @namespace;

        public Serializer(IMessageMapper mapper, Conventions conventions, XmlSerializerCache cache, bool skipWrappingRawXml, string @namespace = DefaultNamespace)
        {
            this.mapper = mapper;
            this.conventions = conventions;
            this.cache = cache;
            this.skipWrappingRawXml = skipWrappingRawXml;
            this.@namespace = @namespace;
        }

        public byte[] Serialize(object message)
        {
            var messageBuilder = SerializeMessage(message);

            var builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" ?>");

            builder.Append(messageBuilder);
            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        string GetNamespace(object message)
        {
            return mapper.GetMappedTypeFor(message.GetType()).Namespace;
        }

        StringBuilder SerializeMessage(object message)
        {
            var messageBuilder = new StringBuilder();
            var t = mapper.GetMappedTypeFor(message.GetType());

            WriteObject(t.SerializationFriendlyName(), t, message, messageBuilder, true);

            return messageBuilder;
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
                return Escape(value as string);
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

        void WriteObject(string name, Type type, object value, StringBuilder builder, bool useNS = false)
        {
            var element = name;

            if (type == typeof(object) && (value.GetType().IsSimpleType()))
            {
                if (!namespacesToAdd.Contains(value.GetType()))
                {
                    namespacesToAdd.Add(value.GetType());
                }

                builder.AppendFormat("<{0}>{1}</{0}>\n",
                    value.GetType().Name.ToLower() + ":" + name,
                    FormatAsString(value));

                return;
            }


            if (useNS)
            {
                var messageNamespace = GetNamespace(value);
                var baseTypes = GetBaseTypes(value);
                CreateStartElementWithNamespaces(messageNamespace, baseTypes, builder, element);
            }
            else
            {
                builder.AppendFormat("<{0}>\n", element);
            }

            Write(builder, type, value);

            builder.AppendFormat("</{0}>\n", element);
        }

        void Write(StringBuilder builder, Type t, object obj)
        {
            if (obj == null)
            {
                return;
            }

            IEnumerable<PropertyInfo> properties;
            if (!cache.typeToProperties.TryGetValue(t, out properties))
            {
                throw new InvalidOperationException(string.Format("Type {0} was not registered in the serializer. Check that it appears in the list of configured assemblies/types to scan.", t.FullName));
            }

            foreach (var prop in properties)
            {
                if (IsIndexedProperty(prop))
                {
                    throw new NotSupportedException(string.Format("Type {0} contains an indexed property named {1}. Indexed properties are not supported on message types.", t.FullName, prop.Name));
                }
                WriteEntry(prop.Name, prop.PropertyType, DelegateFactory.CreateGet(prop).Invoke(obj), builder);
            }

            foreach (var field in cache.typeToFields[t])
            {
                WriteEntry(field.Name, field.FieldType, DelegateFactory.CreateGet(field).Invoke(obj), builder);
            }
        }

        void WriteEntry(string name, Type type, object value, StringBuilder builder)
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
                        WriteEntry(name, typeof(string), "null", builder);
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
                    builder.AppendFormat("{0}\n", container);
                }
                else
                {
                    builder.AppendFormat("<{0}>{1}</{0}>\n", name, container);
                }

                return;
            }

            if (type.IsValueType || type == typeof(string) || type == typeof(Uri) || type == typeof(char))
            {
                builder.AppendFormat("<{0}>{1}</{0}>\n", name, FormatAsString(value));
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                builder.AppendFormat("<{0}>\n", name);

                if (type == typeof(byte[]))
                {
                    var base64String = Convert.ToBase64String((byte[]) value);
                    builder.Append(base64String);
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
                            WriteEntry(obj.GetType().Name, obj.GetType(), obj, builder);
                        }
                        else
                        {
                            WriteObject(baseType.SerializationFriendlyName(), baseType, obj, builder);
                        }
                    }
                }

                builder.AppendFormat("</{0}>\n", name);
                return;
            }

            WriteObject(name, type, value, builder);
        }

        static bool IsIndexedProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo != null)
            {
                return propertyInfo.GetIndexParameters().Length > 0;
            }

            return false;
        }

        void CreateStartElementWithNamespaces(string messageNamespace, List<string> baseTypes, StringBuilder builder, string element)
        {
            builder.AppendFormat(
                "<{0} xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"",
                element);

            builder.AppendFormat(" xmlns=\"{0}/{1}\"", @namespace, messageNamespace);

            foreach (var t in namespacesToAdd)
            {
                builder.AppendFormat(" xmlns:{0}=\"{1}\"", t.Name.ToLower(), t.Name);
            }

            for (var i = 0; i < baseTypes.Count; i++)
            {
                var prefix = BaseType;
                if (i != 0)
                {
                    prefix += i;
                }

                builder.AppendFormat(" xmlns:{0}=\"{1}\"", prefix, baseTypes[i]);
            }

            builder.Append(">\n");
        }
    }
}
