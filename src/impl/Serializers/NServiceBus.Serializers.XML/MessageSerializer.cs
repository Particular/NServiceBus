using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using NServiceBus.Serialization;
using NServiceBus.MessageInterfaces;
using System.Runtime.Serialization;
using Common.Logging;
using NServiceBus.Encryption;
using NServiceBus.Utils.Reflection;
using System.Xml.Serialization;

namespace NServiceBus.Serializers.XML
{
    /// <summary>
    /// Implementation of the message serializer over XML supporting interface-based messages.
    /// </summary>
    public class MessageSerializer : IMessageSerializer
    {
        /// <summary>
        /// The message mapper used to translate between types.
        /// </summary>
        public IMessageMapper MessageMapper
        {
            get { return mapper; }
            set
            {
                mapper = value;

                if (messageTypes != null)
                    mapper.Initialize(messageTypes);
            }
        }

        private IMessageMapper mapper;

        /// <summary>
        /// The encryption service used to encrypt and decrypt WireEncryptedStrings.
        /// </summary>
        public IEncryptionService EncryptionService { get; set; }

        private string nameSpace = "http://tempuri.net";

        /// <summary>
        /// The namespace to place in outgoing XML.
        /// </summary>
        public string Namespace
        {
            get { return nameSpace; }
            set { nameSpace = value; }
        }

        /// <summary>
        /// Gets/sets message types to be serialized
        /// </summary>
        public List<Type> MessageTypes
        {
            get { return messageTypes; }
            set
            {
                messageTypes = value;
                if (!messageTypes.Contains(typeof(EncryptedValue)))
                    messageTypes.Add(typeof(EncryptedValue));

                if (MessageMapper != null)
                    MessageMapper.Initialize(messageTypes.ToArray());

                foreach (Type t in messageTypes)
                    InitType(t);
            }
        }

        private List<Type> messageTypes;

        /// <summary>
        /// Scans the given type storing maps to fields and properties to save on reflection at runtime.
        /// </summary>
        /// <param name="t"></param>
        public void InitType(Type t)
        {
            logger.Debug("Initializing type: " + t.AssemblyQualifiedName);

            if (t.IsSimpleType())
                return;

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                if (t.IsArray)
                    typesToCreateForArrays[t] = typeof(List<>).MakeGenericType(t.GetElementType());

                foreach (Type g in t.GetGenericArguments())
                    InitType(g);

                //Handle dictionaries - initalize relevant KeyValuePair<T,K> types.
                foreach (Type interfaceType in t.GetInterfaces())
                {
                    Type[] arr = interfaceType.GetGenericArguments();
                    if (arr.Length == 1)
                        if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(t))
                            InitType(arr[0]);
                }

                return;
            }

            var isKeyValuePair = false;

            var args = t.GetGenericArguments();
            if (args.Length == 2)
            {
                isKeyValuePair = (typeof (KeyValuePair<,>).MakeGenericType(args) == t);
            }

            if (args.Length == 1 && args[0].IsValueType)
            {
                if (typeof(Nullable<>).MakeGenericType(args) == t)
                {
                    InitType(args[0]);
                    return;
                }
            }

            //already in the process of initializing this type (prevents infinite recursion).
            if (typesBeingInitialized.Contains(t))
                return;

            typesBeingInitialized.Add(t);

            var props = GetAllPropertiesForType(t, isKeyValuePair);
            typeToProperties[t] = props;
            var fields = GetAllFieldsForType(t);
            typeToFields[t] = fields;


            foreach (var p in props)
            {
                logger.Debug("Handling property: " + p.Name);

                propertyInfoToLateBoundProperty[p] = DelegateFactory.Create(p);

                if (!isKeyValuePair)
                    propertyInfoToLateBoundPropertySet[p] = DelegateFactory.CreateSet(p);
            
                InitType(p.PropertyType);
            }

            foreach (var f in fields)
            {
                logger.Debug("Handling field: " + f.Name);

                fieldInfoToLateBoundField[f] = DelegateFactory.Create(f);

                if (!isKeyValuePair)
                    fieldInfoToLateBoundFieldSet[f] = DelegateFactory.CreateSet(f);

                InitType(f.FieldType);
            }
        }

        /// <summary>
        /// Gets a PropertyInfo for each property of the given type.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="isKeyValuePair"></param>
        /// <returns></returns>
        IEnumerable<PropertyInfo> GetAllPropertiesForType(Type t, bool isKeyValuePair)
        {
            var result = new List<PropertyInfo>();

            foreach (var prop in t.GetProperties())
            {
                if (typeof(IList) == prop.PropertyType)
                    throw new NotSupportedException("IList is not a supported property type for serialization, use List instead. Type: " + t.FullName + " Property: " + prop.Name);

                var args = prop.PropertyType.GetGenericArguments();

                if (args.Length == 1)
                    if (typeof(IList<>).MakeGenericType(args) == prop.PropertyType)
                        throw new NotSupportedException("IList<T> is not a supported property type for serialization, use List<T> instead. Type: " + t.FullName + " Property: " + prop.Name);
                
                if (args.Length == 2)
                    if (typeof(IDictionary<,>).MakeGenericType(args) == prop.PropertyType)
                        throw new NotSupportedException("IDictionary<T, K> is not a supported property type for serialization, use Dictionary<T,K> instead. Type: " + t.FullName + " Property: " + prop.Name);

                if (!prop.CanWrite && !isKeyValuePair)
                    continue;
                if (prop.GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Length > 0)
                    continue;

                result.Add(prop);
            }

            if (t.IsInterface)
                foreach (Type interfaceType in t.GetInterfaces())
                    result.AddRange(GetAllPropertiesForType(interfaceType, false));

            return result;
        }

        /// <summary>
        /// Gets a FieldInfo for each field in the given type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        IEnumerable<FieldInfo> GetAllFieldsForType(Type t)
        {
            return t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
        }

        #region Deserialize

        /// <summary>
        /// Deserializes the given stream to an array of messages which are returned.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IMessage[] Deserialize(Stream stream)
        {
            prefixesToNamespaces = new Dictionary<string, string>();
            messageBaseTypes = new List<Type>();
            var result = new List<IMessage>();

            var doc = new XmlDocument { PreserveWhitespace = true };

            doc.Load(XmlReader.Create(stream, new XmlReaderSettings {CheckCharacters = false}));

            if (doc.DocumentElement == null)
                return result.ToArray();

            foreach (XmlAttribute attr in doc.DocumentElement.Attributes)
            {
                if (attr.Name == "xmlns")
                    defaultNameSpace = attr.Value.Substring(attr.Value.LastIndexOf("/") + 1);
                else
                {
                    if (attr.Name.Contains("xmlns:"))
                    {
                        int colonIndex = attr.Name.LastIndexOf(":");
                        string prefix = attr.Name.Substring(colonIndex + 1);

                        if (prefix.Contains(BASETYPE))
                        {
                            Type baseType = MessageMapper.GetMappedTypeFor(attr.Value);
                            if (baseType != null)
                                messageBaseTypes.Add(baseType);
                        }
                        else
                            prefixesToNamespaces[prefix] = attr.Value;
                    }
                }
            }

            if (doc.DocumentElement.Name.ToLower() != "messages")
            {
                object m = Process(doc.DocumentElement, null);

                if (m == null)
                    throw new SerializationException("Could not deserialize message.");

                result.Add(m as IMessage);
            }
            else
            {
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Whitespace)
                        continue;

                    object m = Process(node, null);

                    result.Add(m as IMessage);
                }
            }

            defaultNameSpace = null;

            return result.ToArray();
        }

        private object Process(XmlNode node, object parent)
        {
            string name = node.Name;
            string typeName = defaultNameSpace + "." + name;

            if (name.Contains(":"))
            {
                int colonIndex = node.Name.IndexOf(":");
                name = name.Substring(colonIndex + 1);
                string prefix = node.Name.Substring(0, colonIndex);
                string ns = prefixesToNamespaces[prefix];

                typeName = ns.Substring(nameSpace.LastIndexOf("/") + 1) + "." + name;
            }

            if (name.Contains("NServiceBus."))
                typeName = name;

            if (parent != null)
            {
                if (parent is IEnumerable)
                {
                    if (parent.GetType().IsArray)
                        return GetObjectOfTypeFromNode(parent.GetType().GetElementType(), node);

                    var args = parent.GetType().GetGenericArguments();
                    if (args.Length == 1)
                        return GetObjectOfTypeFromNode(args[0], node);
                }

                PropertyInfo prop = parent.GetType().GetProperty(name);
                if (prop != null)
                    return GetObjectOfTypeFromNode(prop.PropertyType, node);
            }

            Type t = MessageMapper.GetMappedTypeFor(typeName);
            if (t == null)
            {
                logger.Debug("Could not load " + typeName + ". Trying base types...");
                foreach(Type baseType in messageBaseTypes)
                    try
                    {
                        logger.Debug("Trying to deserialize message to " + baseType.FullName);
                        return GetObjectOfTypeFromNode(baseType, node);
                    }
// ReSharper disable EmptyGeneralCatchClause
                    catch { } // intentionally swallow exception
// ReSharper restore EmptyGeneralCatchClause

                throw new TypeLoadException("Could not handle type '" + typeName + "'.");
            }

            return GetObjectOfTypeFromNode(t, node);
        }

        private object GetObjectOfTypeFromNode(Type t, XmlNode node)
        {
            if (t.IsSimpleType() || t == typeof(Uri))
                return GetPropertyValue(t, node);

            if (t == typeof(WireEncryptedString))
            {
                if (EncryptionService != null)
                {
                    var encrypted = GetObjectOfTypeFromNode(typeof (EncryptedValue), node) as EncryptedValue;
                    var s = EncryptionService.Decrypt(encrypted);

                    return new WireEncryptedString {Value = s};
                }

                foreach (XmlNode n in node.ChildNodes)
                    if (n.Name.ToLower() == "encryptedbase64value")
                    {
                        var wes = new WireEncryptedString();
                        wes.Value = GetPropertyValue(typeof (String), n) as string;

                        return wes;
                    }

            }

            if (typeof(IEnumerable).IsAssignableFrom(t))
                return GetPropertyValue(t, node);

            object result = MessageMapper.CreateInstance(t);

            foreach (XmlNode n in node.ChildNodes)
            {
                Type type = null;
                if (n.Name.Contains(":"))
                    type = Type.GetType("System." + n.Name.Substring(0, n.Name.IndexOf(":")), false, true);

                var prop = GetProperty(t, n.Name);
                if (prop != null)
                {
                    var val = GetPropertyValue(type ?? prop.PropertyType, n);
                    if (val != null)
                    {
                        propertyInfoToLateBoundPropertySet[prop].Invoke(result, val);
                        continue;
                    }
                }

                var field = GetField(t, n.Name);
                if (field != null)
                {
                    object val = GetPropertyValue(type ?? field.FieldType, n);
                    if (val != null)
                    {
                        fieldInfoToLateBoundFieldSet[field].Invoke(result, val);
                        continue;
                    }
                }
            }

            return result;
        }

        private static PropertyInfo GetProperty(Type t, string name)
        {
            IEnumerable<PropertyInfo> props;
            typeToProperties.TryGetValue(t, out props);

            if (props == null)
                return null;

            string n = GetNameAfterColon(name);

            foreach (PropertyInfo prop in props)
                if (prop.Name == n)
                    return prop;

            return null;
        }

        private static string GetNameAfterColon(string name)
        {
            var n = name;
            if (name.Contains(":"))
                n = name.Substring(name.IndexOf(":") + 1, name.Length - name.IndexOf(":") - 1);

            return n;
        }

        private FieldInfo GetField(Type t, string name)
        {
            IEnumerable<FieldInfo> fields;
            typeToFields.TryGetValue(t, out fields);

            if (fields == null)
                return null;

            foreach (FieldInfo f in fields)
                if (f.Name == name)
                    return f;

            return null;
        }

        private object GetPropertyValue(Type type, XmlNode n)
        {
            if (n.ChildNodes.Count == 1 && n.ChildNodes[0] is XmlCharacterData)
            {
                var text = n.ChildNodes[0].InnerText;

                var args = type.GetGenericArguments();
                if (args.Length == 1 && args[0].IsValueType)
                {
                    var nullableType = typeof (Nullable<>).MakeGenericType(args);
                    if (type == nullableType)
                    {
                        if (text.ToLower() == "null")
                            return null;

                        return GetPropertyValue(args[0], n);
                    }
                }

                if (type == typeof(string))
                    return text;

                if (type == typeof(Boolean))
                    return XmlConvert.ToBoolean(text);

                if (type == typeof(Byte))
                    return XmlConvert.ToByte(text);

                if (type == typeof(Char))
                    return XmlConvert.ToChar(text);

                if (type == typeof(DateTime))
                    return XmlConvert.ToDateTime(text, XmlDateTimeSerializationMode.RoundtripKind);

                if (type == typeof(DateTimeOffset))
                    return XmlConvert.ToDateTimeOffset(text); 

                if (type == typeof(decimal))
                    return XmlConvert.ToDecimal(text);

                if (type == typeof(double))
                    return XmlConvert.ToDouble(text);

                if (type == typeof(Guid))
                    return XmlConvert.ToGuid(text);

                if (type == typeof(Int16))
                    return XmlConvert.ToInt16(text);

                if (type == typeof(Int32))
                    return XmlConvert.ToInt32(text);

                if (type == typeof(Int64))
                    return XmlConvert.ToInt64(text);

                if (type == typeof(sbyte))
                    return XmlConvert.ToSByte(text);

                if (type == typeof(Single))
                    return XmlConvert.ToSingle(text);

                if (type == typeof(TimeSpan))
                    return XmlConvert.ToTimeSpan(text);

                if (type == typeof(UInt16))
                    return XmlConvert.ToUInt16(text);

                if (type == typeof(UInt32))
                    return XmlConvert.ToUInt32(text);

                if (type == typeof(UInt64))
                    return XmlConvert.ToUInt64(text);

                if (type.IsEnum)
                    return Enum.Parse(type, text);

                if (type == typeof(byte[]))
                    return Convert.FromBase64String(text);

                if (type == typeof(Uri))
                    return new Uri(text);

                if (!(n.ChildNodes[0] is XmlWhitespace))
                    throw new Exception("Type not supported by the serializer: " + type.AssemblyQualifiedName);
            }

            //Handle dictionaries
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                var result = Activator.CreateInstance(type) as IDictionary;

                var keyType = typeof (object);
                var valueType = typeof (object);

                foreach(var interfaceType in type.GetInterfaces())
                {
                    var args = interfaceType.GetGenericArguments();
                    if (args.Length == 2)
                        if (typeof(IDictionary<,>).MakeGenericType(args).IsAssignableFrom(type))
                        {
                            keyType = args[0];
                            valueType = args[1];
                            break;
                        }
                }

                foreach (XmlNode xn in n.ChildNodes) // go over KeyValuePairs
                {
                    object key = null;
                    object value = null;

                    foreach (XmlNode node in xn.ChildNodes)
                    {
                        if (node.Name == "Key")
                            key = GetObjectOfTypeFromNode(keyType, node);
                        if (node.Name == "Value")
                            value = GetObjectOfTypeFromNode(valueType, node);
                    }

                    if (result != null && key != null) result[key] = value;
                }

                return result;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
				var list = CreateList(type);
                if (list != null)
                {
                    foreach (XmlNode xn in n.ChildNodes)
                    {
                      if (xn.NodeType == XmlNodeType.Whitespace)
                        continue;

                        object m = Process(xn, list);

                        list.Add(m);
                    }

                    if (type.IsArray)
                        return list.GetType().GetMethod("ToArray").Invoke(list, null);
                }

                return list;
            }

            if (n.ChildNodes.Count == 0)
                if (type == typeof(string))
                    return string.Empty;
                else
                    return null;


            return GetObjectOfTypeFromNode(type, n);
        }

		private IList CreateList(Type type)
		{
			Type typeToCreate = type;
			if (type.IsArray)
				typeToCreate = typesToCreateForArrays[type];

			if (typeof (IList).IsAssignableFrom(typeToCreate))
			{
				return Activator.CreateInstance(typeToCreate) as IList;
			}

			return null;
		}

    	#endregion

        #region Serialize

        /// <summary>
        /// Serializes the given messages to the given stream.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="stream"></param>
        public void Serialize(IMessage[] messages, Stream stream)
        {
            namespacesToPrefix = new Dictionary<string, string>();
            namespacesToAdd = new List<Type>();

            var namespaces = GetNamespaces(messages, MessageMapper);
            for (int i = 0; i < namespaces.Count; i++)
            {
                string prefix = "q" + i;
                if (i == 0)
                    prefix = "";

                if (namespaces[i] != null)
                    namespacesToPrefix[namespaces[i]] = prefix;
            }

            var messageBuilder = new StringBuilder();
            foreach (var m in messages)
            {
                var t = MessageMapper.GetMappedTypeFor(m.GetType());

                WriteObject(t.Name, t, m, messageBuilder);
            }

            var builder = new StringBuilder();

            List<string> baseTypes = GetBaseTypes(messages, MessageMapper);

            builder.AppendLine("<?xml version=\"1.0\" ?>");

            builder.Append("<Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"");

            for (int i = 0; i < namespaces.Count; i++)
            {
                string prefix = "q" + i;
                if (i == 0)
                    prefix = "";

                builder.AppendFormat(" xmlns{0}=\"{1}/{2}\"", (prefix != "" ? ":" + prefix : prefix), nameSpace, namespaces[i]);
            }

            foreach (var type in namespacesToAdd)
                builder.AppendFormat(" xmlns:{0}=\"{1}\"", type.Name.ToLower(), type.Name);

            for (int i = 0; i < baseTypes.Count; i++)
            {
                string prefix = BASETYPE;
                if (i != 0)
                    prefix += i;

                builder.AppendFormat(" xmlns:{0}=\"{1}\"", prefix, baseTypes[i]);
            }

            builder.Append(">\n");

            builder.Append(messageBuilder.ToString());

            builder.AppendLine("</Messages>");

            byte[] buffer = Encoding.UTF8.GetBytes(builder.ToString());
            stream.Write(buffer, 0, buffer.Length);
        }

        private void Write(StringBuilder builder, Type t, object obj)
        {
            if (obj == null)
                return;

            if (!typeToProperties.ContainsKey(t))
                throw new InvalidOperationException("Type " + t.FullName + " was not registered in the serializer. Check that it appears in the list of configured assemblies/types to scan.");

            foreach (PropertyInfo prop in typeToProperties[t])
                WriteEntry(prop.Name, prop.PropertyType, propertyInfoToLateBoundProperty[prop].Invoke(obj), builder);

            foreach(FieldInfo field in typeToFields[t])
                WriteEntry(field.Name, field.FieldType, fieldInfoToLateBoundField[field].Invoke(obj), builder);
        }

        private void WriteObject(string name, Type type, object value, StringBuilder builder)
        {
            if (value is WireEncryptedString)
            {
                if (EncryptionService == null)
                    throw new InvalidOperationException(String.Format("Cannot encrypt field {0} because no encryption service was configured.", name));
                
                var encryptedValue = EncryptionService.Encrypt((value as WireEncryptedString).Value);
                WriteObject(name, typeof(EncryptedValue), encryptedValue, builder);

                return;
            }

            if (value is Uri)
            {
                builder.AppendFormat("<{0}>{1}</{0}>\n", name, value);
                return;
            }

            string element = name;
            string prefix;
            namespacesToPrefix.TryGetValue(type.Namespace, out prefix);

            if (string.IsNullOrEmpty(prefix) && type == typeof(object) && (value.GetType().IsSimpleType()))
            {
                if (!namespacesToAdd.Contains(value.GetType()))
                    namespacesToAdd.Add(value.GetType());

                builder.AppendFormat("<{0}>{1}</{0}>\n", 
                    value.GetType().Name.ToLower() + ":" + name,
                    FormatAsString(value));

                return;
            }

            if (!string.IsNullOrEmpty(prefix))
                element = prefix + ":" + name;

            builder.AppendFormat("<{0}>\n", element);

            Write(builder, type, value);

            builder.AppendFormat("</{0}>\n", element);
        }

        private void WriteEntry(string name, Type type, object value, StringBuilder builder)
        {
            if (value == null)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                    return;

                var args = type.GetGenericArguments();
                if (args.Length == 1)
                {
                    var nullableType = typeof (Nullable<>).MakeGenericType(args);
                    if (type == nullableType)
                    {
                        WriteEntry(name, typeof(string), "null", builder);
                        return;
                    }
                }

                return;
            }

            if (type.IsValueType || type == typeof(string))
            {
                builder.AppendFormat("<{0}>{1}</{0}>\n", name, FormatAsString(value));
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                builder.AppendFormat("<{0}>\n", name);

                if (type == typeof(byte[]))
                {
                    var str = Convert.ToBase64String((byte[])value);
                    builder.Append(str);
                }
                else
                {
                    Type baseType = typeof(object);

                    //Get generic type from list: T for List<T>, KeyValuePair<T,K> for IDictionary<T,K>
                    foreach (Type interfaceType in type.GetInterfaces())
                    {
                        Type[] arr = interfaceType.GetGenericArguments();
                        if (arr.Length == 1)
                            if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(type))
                            {
                                baseType = arr[0];
                                break;
                            }
                    }


                    foreach (object obj in ((IEnumerable)value))
                        if (obj.GetType().IsSimpleType())
                            WriteEntry(obj.GetType().Name, obj.GetType(), obj, builder);
                        else
                            WriteObject(baseType.SerializationFriendlyName(), baseType, obj, builder);

                }

                builder.AppendFormat("</{0}>\n", name);
                return;
            }

            WriteObject(name, type, value, builder);
        }

        private static string FormatAsString(object value)
        {
            if (value is bool)
                return XmlConvert.ToString((bool) value);
            if (value is byte)
                return XmlConvert.ToString((byte) value);
            if (value is char)
                return XmlConvert.ToString((char) value);
            if (value is double)
                return XmlConvert.ToString((double) value);
            if (value is ulong)
                return XmlConvert.ToString((ulong) value);
            if (value is uint)
                return XmlConvert.ToString((uint) value);
            if (value is ushort)
                return XmlConvert.ToString((ushort) value);
            if (value is long)
                return XmlConvert.ToString((long) value);
            if (value is int)
                return XmlConvert.ToString((int) value);
            if (value is short)
                return XmlConvert.ToString((short) value);
            if (value is sbyte)
                return XmlConvert.ToString((sbyte) value);
            if (value is decimal)
                return XmlConvert.ToString((decimal) value);
            if (value is float)
                return XmlConvert.ToString((float) value);
            if (value is Guid)
                return XmlConvert.ToString((Guid) value);
            if (value is DateTime)
                return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
            if (value is DateTimeOffset)
                return XmlConvert.ToString((DateTimeOffset)value);
            if (value is TimeSpan)
                return XmlConvert.ToString((TimeSpan) value);
            if (value is string)
                return System.Security.SecurityElement.Escape(value as string);

            return value.ToString();
        }

        private static List<string> GetNamespaces(IMessage[] messages, IMessageMapper mapper)
        {
            var result = new List<string>();

            foreach (IMessage m in messages)
            {
                string ns = mapper.GetMappedTypeFor(m.GetType()).Namespace;
                if (!result.Contains(ns))
                    result.Add(ns);
            }

            return result;
        }

        private static List<string> GetBaseTypes(IMessage[] messages, IMessageMapper mapper)
        {
            var result = new List<string>();

            foreach (IMessage m in messages)
            {
                Type t = mapper.GetMappedTypeFor(m.GetType());

                Type baseType = t.BaseType;
                while (baseType != typeof(object) && baseType != null)
                {
                    if (typeof(IMessage).IsAssignableFrom(baseType))
                        if (!result.Contains(baseType.FullName))
                            result.Add(baseType.FullName);

                    baseType = baseType.BaseType;
                }

                foreach (Type i in t.GetInterfaces())
                    if (i != typeof(IMessage) && typeof(IMessage).IsAssignableFrom(i))
                        if (!result.Contains(i.FullName))
                            result.Add(i.FullName);
            }

            return result;
        }

        #endregion

        #region members

        private const string XMLPREFIX = "d1p1";
        private const string XMLTYPE = XMLPREFIX + ":type";
        private const string BASETYPE = "baseType";

        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> typeToProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly Dictionary<Type, IEnumerable<FieldInfo>> typeToFields = new Dictionary<Type, IEnumerable<FieldInfo>>();
        private static readonly Dictionary<Type, Type> typesToCreateForArrays = new Dictionary<Type, Type>();
        private static readonly List<Type> typesBeingInitialized = new List<Type>();

        private static readonly Dictionary<PropertyInfo, LateBoundProperty> propertyInfoToLateBoundProperty = new Dictionary<PropertyInfo, LateBoundProperty>();
        private static readonly Dictionary<FieldInfo, LateBoundField> fieldInfoToLateBoundField = new Dictionary<FieldInfo, LateBoundField>();
        private static readonly Dictionary<PropertyInfo, LateBoundPropertySet> propertyInfoToLateBoundPropertySet = new Dictionary<PropertyInfo, LateBoundPropertySet>();
        private static readonly Dictionary<FieldInfo, LateBoundFieldSet> fieldInfoToLateBoundFieldSet = new Dictionary<FieldInfo, LateBoundFieldSet>();

        [ThreadStatic]
        private static string defaultNameSpace;

        /// <summary>
        /// Used for serialization
        /// </summary>
        [ThreadStatic]
        private static IDictionary<string, string> namespacesToPrefix;

        /// <summary>
        /// Used for deserialization
        /// </summary>
        [ThreadStatic]
        private static IDictionary<string, string> prefixesToNamespaces;

        [ThreadStatic]
        private static List<Type> messageBaseTypes;

        [ThreadStatic]
        private static List<Type> namespacesToAdd;

        private static readonly ILog logger = LogManager.GetLogger("NServiceBus.Serializers.XML");

        #endregion
    }
}
