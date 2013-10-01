namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Logging;
    using MessageInterfaces;
    using Serialization;
    using Utils.Reflection;

    /// <summary>
    /// Implementation of the message serializer over XML supporting interface-based messages.
    /// </summary>
    public class XmlMessageSerializer : IMessageSerializer
    {
        IMessageMapper mapper;

        /// <summary>
        /// The namespace to place in outgoing XML.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// If true, then the serializer will use a sanitizing stream to skip invalid characters from the stream before parsing
        /// </summary>
        public bool SanitizeInput { get; set; }

        /// <summary>
        /// Removes the wrapping "<Messages/>" element if serializing a single message 
        /// </summary>
        public bool SkipWrappingElementForSingleMessages { get; set; }

        /// <summary>
        /// Removes the wrapping of properties containing XDocument or XElement with property name as root element
        /// </summary>
        public bool SkipWrappingRawXml { get; set; }

        object locker = new object();

        public void InitType(Type t)
        {
            GetTypeMetadata(t);
        }

        TypeMetaData GetTypeMetadata(Type t)
        {
            TypeMetaData typeMetaData;
            if (metaDatas.TryGetValue(t, out typeMetaData))
            {
                return typeMetaData;
            }
            lock (locker)
            {
                return InnerGetTypeMetadata(t);
            }
        }

        TypeMetaData InnerGetTypeMetadata(Type t)
        {
            TypeMetaData typeMetaData;
            if (metaDatas.TryGetValue(t, out typeMetaData))
            {
                return typeMetaData;
            }

            typeMetaData = metaDatas[t] = new TypeMetaData();
            logger.Debug("Initializing type: " + t.AssemblyQualifiedName);

            if (t.IsSimpleType())
            {
                typeMetaData.IsSimpleType = true;
                return typeMetaData;
            }

            if (typeof(XContainer).IsAssignableFrom(t))
            {
                typeMetaData.IsXContainer = true;
                return typeMetaData;
            }

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                foreach (var g in t.GetGenericArguments())
                {
                    InnerGetTypeMetadata(g);
                }

                //Handle dictionaries - initialize relevant KeyValuePair<T,K> types.
                foreach (var interfaceType in t.GetInterfaces())
                {
                    var arr = interfaceType.GetGenericArguments();
                    if (arr.Length == 1)
                    {
                        if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(t))
                        {
                            InnerGetTypeMetadata(arr[0]);
                        }
                    }
                }

                if (t.IsArray)
                {
                    typeMetaData.ListType = typeof(List<>).MakeGenericType(t.GetElementType());
                    typeMetaData.IsArray = true;
                    return typeMetaData;
                }

                if (t.IsSet())
                {
                    typeMetaData.ListType = typeof(List<>).MakeGenericType(t.GetGenericArguments());
                    typeMetaData.IsSet = true;
                    return typeMetaData;
                }
                if (t.IsGenericEnumerable())
                {
                    typeMetaData.ListType = typeof(List<>).MakeGenericType(t.GetGenericArguments());
                    typeMetaData.IsGenericEnumerable = true;
                    return typeMetaData;
                }
                if (t.IsList())
                {
                    typeMetaData.ListType = t;
                    typeMetaData.IsList = true;
                    return typeMetaData;
                }
                return typeMetaData;
            }

            var isKeyValuePair = false;

            var args = t.GetGenericArguments();
            if (args.Length == 2)
            {
                isKeyValuePair = (typeof(KeyValuePair<,>).MakeGenericType(args) == t);
            }

            if (args.Length == 1 && args[0].IsValueType)
            {
                if (args[0].GetGenericArguments().Any() || typeof(Nullable<>).MakeGenericType(args) == t)
                {
                    InnerGetTypeMetadata(args[0]);

                    if (!args[0].GetGenericArguments().Any())
                    {
                        return typeMetaData;
                    }
                }
            }

            foreach (var property in GetAllPropertiesForType(t, isKeyValuePair))
            {
                //todo: throw on IsIndexedProperty
                var propertyMetaData = new PropertyMetaData();
                typeMetaData.Properties.Add(property.Name, propertyMetaData);
                logger.Debug("Handling property: " + property.Name);

                propertyMetaData.LateBoundProperty = DelegateFactory.Create(property);
                propertyMetaData.PropertyType = property.PropertyType;

                if (!isKeyValuePair)
                {
                    propertyMetaData.LateBoundPropertySet = DelegateFactory.CreateSet(property);
                }
                propertyMetaData.IsIndexed = property.GetIndexParameters().Length > 0;

                InnerGetTypeMetadata(property.PropertyType);
            }
            foreach (var field in GetAllFieldsForType(t))
            {
                var fieldMetaData = new FieldMetaData();
                typeMetaData.Fields.Add(field.Name, fieldMetaData);
                logger.Debug("Handling field: " + field.Name);

                fieldMetaData.LateBoundField = DelegateFactory.Create(field);
                fieldMetaData.FieldType = field.FieldType;

                if (!isKeyValuePair)
                {
                    fieldMetaData.LateBoundFieldSet = DelegateFactory.CreateSet(field);
                }

                InnerGetTypeMetadata(field.FieldType);
            }
            return typeMetaData;
        }

        IEnumerable<PropertyInfo> GetAllPropertiesForType(Type t, bool isKeyValuePair)
        {
            var result = new List<PropertyInfo>();

            foreach (var prop in t.GetProperties())
            {
                if (!prop.CanWrite && !isKeyValuePair)
                {
                    continue;
                }

                if (prop.GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Length > 0)
                {
                    continue;
                }

                prop.VerifyCanBeSerialized();

                result.Add(prop);
            }

            if (t.IsInterface)
            {
                foreach (var interfaceType in t.GetInterfaces())
                {
                    result.AddRange(GetAllPropertiesForType(interfaceType, false));
                }
            }

            return result.Distinct();
        }


        IEnumerable<FieldInfo> GetAllFieldsForType(Type t)
        {
            return t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream">Stream that contains messages.</param>
        /// <param name="messageTypesToDeserialize">The list of message types to deserialize. If null the types must be inferred from the serialized data.</param>
        /// <returns>Deserialized messages.</returns>
        public object[] Deserialize(Stream stream, IList<Type> messageTypesToDeserialize = null)
        {
            if (stream == null)
                return null;

            prefixesToNamespaces = new Dictionary<string, string>();
            messageBaseTypes = new List<Type>();
            var result = new List<object>();

            var doc = new XmlDocument { PreserveWhitespace = true };

            var reader = SanitizeInput
                                  ? XmlReader.Create(new XmlSanitizingStream(stream), new XmlReaderSettings { CheckCharacters = false })
                                  : XmlReader.Create(stream, new XmlReaderSettings { CheckCharacters = false });

            doc.Load(reader);

            if (doc.DocumentElement == null)
            {
                return result.ToArray();
            }

            foreach (XmlAttribute attr in doc.DocumentElement.Attributes)
            {
                if (attr.Name == "xmlns")
                    defaultNameSpace = attr.Value.Substring(attr.Value.LastIndexOf("/") + 1);
                else
                {
                    if (attr.Name.Contains("xmlns:"))
                    {
                        var colonIndex = attr.Name.LastIndexOf(":");
                        var prefix = attr.Name.Substring(colonIndex + 1);

                        if (prefix.Contains(BASETYPE))
                        {
                            var baseType = mapper.GetMappedTypeFor(attr.Value);
                            if (baseType != null)
                            {
                                messageBaseTypes.Add(baseType);
                            }
                        }
                        else
                        {
                            prefixesToNamespaces[prefix] = attr.Value;
                        }
                    }
                }
            }

            if (doc.DocumentElement.Name.ToLower() != "messages")
            {
                Type nodeType = null;

                if (messageTypesToDeserialize != null)
                {
                    nodeType = messageTypesToDeserialize.FirstOrDefault();
                }

                var m = Process(doc.DocumentElement, null, nodeType);

                if (m == null)
                {
                    throw new SerializationException("Could not deserialize message.");
                }

                result.Add(m);
            }
            else
            {
                var position = 0;
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Whitespace)
                        continue;


                    Type nodeType = null;

                    if (messageTypesToDeserialize != null && position < messageTypesToDeserialize.Count)
                    {
                        nodeType = messageTypesToDeserialize.ElementAt(position);
                    }


                    var m = Process(node, null, nodeType);

                    result.Add(m);

                    position++;
                }
            }

            defaultNameSpace = null;

            return result.ToArray();
        }

        object Process(XmlNode node, object parent, Type nodeType = null)
        {
            if (nodeType == null)
            {
                nodeType = InferNodeType(node, parent);
            }

            return GetObjectOfTypeFromNode(nodeType, node);
        }

        Type InferNodeType(XmlNode node, object parent)
        {
            var name = node.Name;
            var typeName = name;

            if (!string.IsNullOrEmpty(defaultNameSpace))
            {
                typeName = defaultNameSpace + "." + typeName;
            }

            if (name.Contains(":"))
            {
                var colonIndex = node.Name.IndexOf(":");
                name = name.Substring(colonIndex + 1);
                var prefix = node.Name.Substring(0, colonIndex);
                var ns = prefixesToNamespaces[prefix];

                typeName = ns.Substring(ns.LastIndexOf("/") + 1) + "." + name;
            }

            if (name.Contains("NServiceBus."))
            {
                typeName = name;
            }


            if (parent != null)
            {
                if (parent is IEnumerable)
                {
                    if (parent.GetType().IsArray)
                        return parent.GetType().GetElementType();
                    
					var listImplementations = parent.GetType().GetInterfaces().Where(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IList<>)).ToList();
					if (listImplementations.Any())
					{
						var listImplementation = listImplementations.First();
						return listImplementation.GetGenericArguments().Single();
					}

	                var args = parent.GetType().GetGenericArguments();

                    if (args.Length == 1)
                    {
                        return args[0];
                    }
                }

                var prop = parent.GetType().GetProperty(name);

                if (prop != null)
                {
                    return prop.PropertyType;
                }
            }

            var mappedType = mapper.GetMappedTypeFor(typeName);

            if (mappedType != null)
                return mappedType;


            logger.Debug("Could not load " + typeName + ". Trying base types...");
            foreach (var baseType in messageBaseTypes)
            {
                try
                {
                    logger.Debug("Trying to deserialize message to " + baseType.FullName);
                    return baseType;
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch
                {
                    // intentionally swallow exception
                } 
                // ReSharper restore EmptyGeneralCatchClause
            }

            throw new TypeLoadException("Could not determine type for node: '" + node.Name + "'.");
        }

        object GetObjectOfTypeFromNode(Type t, XmlNode node)
        {
            if (t.IsSimpleType() || t == typeof(Uri))
            {
                return GetPropertyValue(t, node);
            }

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                return GetPropertyValue(t, node);
            }

            var result = mapper.CreateInstance(t);

            foreach (XmlNode n in node.ChildNodes)
            {
                Type type = null;
                if (n.Name.Contains(":"))
                {
                    type = Type.GetType("System." + n.Name.Substring(0, n.Name.IndexOf(":")), false, true);
                }

                var typeMetaData = GetTypeMetadata(t);
                PropertyMetaData property;
                if (typeMetaData.Properties.TryGetValue(n.Name, out property))
                {
                    var val = GetPropertyValue(type ?? property.PropertyType, n);
                    if (val != null)
                    {
                        property.LateBoundPropertySet.Invoke(result, val);
                        continue;
                    }
                }


                FieldMetaData field;
                if (typeMetaData.Fields.TryGetValue(n.Name, out field))
                {
                    var val = GetPropertyValue(type ?? field.FieldType, n);
                    if (val != null)
                    {
                        field.LateBoundFieldSet.Invoke(result, val);
                        continue;
                    }
                }

            }

            return result;
        }

        static string GetNameAfterColon(string name)
        {
            if (name.Contains(":"))
            {
                return name.Substring(name.IndexOf(":") + 1, name.Length - name.IndexOf(":") - 1);
            }
            return name;
        }


        object GetPropertyValue(Type type, XmlNode n)
        {
            if ((n.ChildNodes.Count == 1) && (n.ChildNodes[0] is XmlCharacterData))
            {
                var text = n.ChildNodes[0].InnerText;

                var args = type.GetGenericArguments();

                if (args.Length == 1 && args[0].IsValueType)
                {
                    if (args[0].GetGenericArguments().Any())
                    {
                        return GetPropertyValue(args[0], n);
                    }

                    var nullableType = typeof(Nullable<>).MakeGenericType(args);
                    if (type == nullableType)
                    {
                        if (text.ToLower() == "null")
                            return null;

                        return GetPropertyValue(args[0], n);
                    }
                }

                if (type == typeof(string))
                {
                    return text;
                }

                if (type == typeof(Boolean))
                {
                    return XmlConvert.ToBoolean(text);
                }

                if (type == typeof(Byte))
                {
                    return XmlConvert.ToByte(text);
                }

                if (type == typeof(Char))
                {
                    return XmlConvert.ToChar(text);
                }

                if (type == typeof(DateTime))
                {
                    return XmlConvert.ToDateTime(text, XmlDateTimeSerializationMode.RoundtripKind);
                }

                if (type == typeof(DateTimeOffset))
                {
                    return XmlConvert.ToDateTimeOffset(text);
                }

                if (type == typeof(decimal))
                {
                    return XmlConvert.ToDecimal(text);
                }

                if (type == typeof(double))
                {
                    return XmlConvert.ToDouble(text);
                }

                if (type == typeof(Guid))
                {
                    return XmlConvert.ToGuid(text);
                }

                if (type == typeof(Int16))
                {
                    return XmlConvert.ToInt16(text);
                }

                if (type == typeof(Int32))
                {
                    return XmlConvert.ToInt32(text);
                }

                if (type == typeof(Int64))
                {
                    return XmlConvert.ToInt64(text);
                }

                if (type == typeof(sbyte))
                {
                    return XmlConvert.ToSByte(text);
                }

                if (type == typeof(Single))
                {
                    return XmlConvert.ToSingle(text);
                }

                if (type == typeof(TimeSpan))
                {
                    return XmlConvert.ToTimeSpan(text);
                }

                if (type == typeof(UInt16))
                {
                    return XmlConvert.ToUInt16(text);
                }

                if (type == typeof(UInt32))
                {
                    return XmlConvert.ToUInt32(text);
                }

                if (type == typeof(UInt64))
                {
                    return XmlConvert.ToUInt64(text);
                }

                if (type.IsEnum)
                {
                    return Enum.Parse(type, text);
                }

                if (type == typeof(byte[]))
                {
                    return Convert.FromBase64String(text);
                }

                if (type == typeof(Uri))
                {
                    return new Uri(text);
                }

                if (!typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (n.ChildNodes[0] is XmlWhitespace)
                    {
                        return Activator.CreateInstance(type);
                    }

                    throw new Exception("Type not supported by the serializer: " + type.AssemblyQualifiedName);
                }
            }

            if (typeof(XContainer).IsAssignableFrom(type))
            {
                var reader = new StringReader(SkipWrappingRawXml ? n.OuterXml : n.InnerXml);

                if (type == typeof(XDocument))
                {
                    return XDocument.Load(reader);
                }

                return XElement.Load(reader);
            }

            //Handle dictionaries
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                var result = Activator.CreateInstance(type) as IDictionary;

                var keyType = typeof(object);
                var valueType = typeof(object);

                foreach (var interfaceType in type.GetInterfaces())
                {
                    var args = interfaceType.GetGenericArguments();
                    if (args.Length == 2)
                    {
                        if (typeof(IDictionary<,>).MakeGenericType(args).IsAssignableFrom(type))
                        {
                            keyType = args[0];
                            valueType = args[1];
                            break;
                        }
                    }
                }

                foreach (XmlNode xn in n.ChildNodes) // go over KeyValuePairs
                {
                    object key = null;
                    object value = null;

                    foreach (XmlNode node in xn.ChildNodes)
                    {
                        if (node.Name == "Key")
                        {
                            key = GetObjectOfTypeFromNode(keyType, node);
                        }
                        if (node.Name == "Value")
                        {
                            value = GetObjectOfTypeFromNode(valueType, node);
                        }
                    }

                    if (result != null && key != null)
                    {
                        result[key] = value;
                    }
                }

                return result;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {

                var typeMetaData = GetTypeMetadata(type);
             
                if (typeMetaData.IsSet)
                {
                    var list = GetListFromNode(n, typeMetaData.ListType);
                    return Activator.CreateInstance(type, typeMetaData.ListType.GetMethod("ToArray").Invoke(list, null));
                }
                if (typeMetaData.IsArray)
                {
                    var list = GetListFromNode(n, typeMetaData.ListType);
                    return typeMetaData.ListType.GetMethod("ToArray").Invoke(list, null);
                }
                if (typeMetaData.IsArray)
                {
                    var list = GetListFromNode(n, typeMetaData.ListType);
                    return typeMetaData.ListType.GetMethod("ToArray").Invoke(list, null);
                }
                if (typeMetaData.IsList || typeMetaData.IsGenericEnumerable)
                {
                    return GetListFromNode(n, typeMetaData.ListType);
                }
                if (typeof(IList).IsAssignableFrom(type))
                {
                    return GetListFromNode(n, type);
                }
            }

            if (n.ChildNodes.Count == 0)
            {
                if (type == typeof(string))
                {
                    return string.Empty;
                }
                return null;
            }

            return GetObjectOfTypeFromNode(type, n);
        }

        IList GetListFromNode(XmlNode n, Type listType)
        {
            var list = (IList) Activator.CreateInstance(listType);
            foreach (XmlNode xn in n.ChildNodes)
            {
                if (xn.NodeType == XmlNodeType.Whitespace)
                {
                    continue;
                }

                var m = Process(xn, list);
                list.Add(m);
            }
            return list;
        }

        /// <summary>
        /// Serializes the given messages to the given stream.
        /// </summary>
        public void Serialize(object[] messages, Stream stream)
        {
            var namespaces = InitializeNamespaces(messages);
            var messageBuilder = SerializeMessages(messages);

            var builder = new StringBuilder();


            builder.AppendLine("<?xml version=\"1.0\" ?>");

            if (SkipWrappingElementForSingleMessages && messages.Length == 1)
            {
                builder.Append(messageBuilder);
            }
            else
            {
                var baseTypes = GetBaseTypes(messages);

                WrapMessages(builder, namespaces, baseTypes, messageBuilder);
            }
            var buffer = Encoding.UTF8.GetBytes(builder.ToString());
            stream.Write(buffer, 0, buffer.Length);
        }

        public string ContentType { get { return ContentTypes.Xml; } }

        void WrapMessages(StringBuilder builder, List<string> namespaces, List<string> baseTypes, StringBuilder messageBuilder)
        {
            CreateStartElementWithNamespaces(namespaces, baseTypes, builder, "Messages");

            builder.Append(messageBuilder);

            builder.AppendLine("</Messages>");
        }

        StringBuilder SerializeMessages(object[] messages)
        {
            var messageBuilder = new StringBuilder();

            foreach (var m in messages)
            {
                var t = mapper.GetMappedTypeFor(m.GetType());

                WriteObject(t.Name, t, m, messageBuilder, SkipWrappingElementForSingleMessages && messages.Length == 1);
            }
            return messageBuilder;
        }

        List<string> InitializeNamespaces(object[] messages)
        {
            namespacesToPrefix = new Dictionary<string, string>();
            namespacesToAdd = new List<Type>();

            var namespaces = GetNamespaces(messages);
            for (var i = 0; i < namespaces.Count; i++)
            {
                var prefix = "q" + i;
                if (i == 0)
                {
                    prefix = "";
                }

                if (namespaces[i] != null)
                {
                    namespacesToPrefix[namespaces[i]] = prefix;
                }
            }
            return namespaces;
        }

        void Write(StringBuilder builder, Type t, object obj)
        {
            if (obj == null)
            {
                return;
            }
            var typeMetaData =  GetTypeMetadata(t);

            foreach (var prop in typeMetaData.Properties)
            {
                WriteEntry(prop.Key, prop.Value.PropertyType,prop.Value.LateBoundProperty.Invoke(obj), builder);
            }

            foreach (var field in typeMetaData.Fields)
            {
                WriteEntry(field.Key, field.Value.FieldType, field.Value.LateBoundField.Invoke(obj), builder);
            }
        }

        void WriteObject(string name, Type type, object value, StringBuilder builder, bool useNS = false)
        {
            var element = name;
            string prefix;
            namespacesToPrefix.TryGetValue(type.Namespace, out prefix);

            if (string.IsNullOrEmpty(prefix) && type == typeof(object) && (value.GetType().IsSimpleType()))
            {
                if (!namespacesToAdd.Contains(value.GetType()))
                {
                    namespacesToAdd.Add(value.GetType());
                }
                
                builder.AppendFormat("<{0}>{1}</{0}>\n",
                    value.GetType().Name.ToLower() + ":" + name,
                    XmlConverter.ConvertToString(value));

                return;
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                element = prefix + ":" + name;
            }

            if (useNS)
            {
                var namespaces = InitializeNamespaces(new[] {value});
                var baseTypes = GetBaseTypes(new[] {value});
                CreateStartElementWithNamespaces(namespaces, baseTypes, builder, element);
            }
            else
            {
                builder.AppendFormat("<{0}>\n", element);
            }

            Write(builder, type, value);

            builder.AppendFormat("</{0}>\n", element);
        }

        void CreateStartElementWithNamespaces(List<string> namespaces, List<string> baseTypes, StringBuilder builder, string element)
        {
            string prefix;

            builder.AppendFormat(
                "<{0} xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"",
                element);

            for (var i = 0; i < namespaces.Count; i++)
            {
                prefix = "q" + i;
                if (i == 0)
                {
                    prefix = "";
                }

                builder.AppendFormat(" xmlns{0}=\"{1}/{2}\"", (prefix != "" ? ":" + prefix : prefix), Namespace,
                                     namespaces[i]);
            }

            foreach (var t in namespacesToAdd)
            {
                builder.AppendFormat(" xmlns:{0}=\"{1}\"", t.Name.ToLower(), t.Name);
            }

            for (var i = 0; i < baseTypes.Count; i++)
            {
                prefix = BASETYPE;
                if (i != 0)
                {
                    prefix += i;
                }

                builder.AppendFormat(" xmlns:{0}=\"{1}\"", prefix, baseTypes[i]);
            }

            builder.Append(">\n");
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
                var container = (XContainer)value;
                if(SkipWrappingRawXml)
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
                builder.AppendFormat("<{0}>{1}</{0}>\n", name, XmlConverter.ConvertToString(value));
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                builder.AppendFormat("<{0}>\n", name);

                if (type == typeof(byte[]))
                {
                    var base64String = Convert.ToBase64String((byte[])value);
                    builder.Append(base64String);
                }
                else
                {
                    var baseType = typeof(object);

                    var interfaces = type.GetInterfaces();
                    if (type.IsInterface)
                    {
                        interfaces = interfaces.Union(new[] { type }).ToArray();
                    }

                    //Get generic type from list: T for List<T>, KeyValuePair<T,K> for IDictionary<T,K>
                    foreach (var interfaceType in interfaces)
                    {
                        var arr = interfaceType.GetGenericArguments();
                        if (arr.Length == 1)
                        {
                            if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(type))
                            {
                                baseType = arr[0];
                                break;
                            }
                        }
                    }


                    foreach (var obj in ((IEnumerable)value))
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



        List<string> GetNamespaces(object[] messages)
        {
            var result = new List<string>();

            foreach (var m in messages)
            {
                var ns = mapper.GetMappedTypeFor(m.GetType()).Namespace;
                if (!result.Contains(ns))
                {
                    result.Add(ns);
                }
            }

            return result;
        }

        List<string> GetBaseTypes(object[] messages)
        {
            var result = new List<string>();

            foreach (var m in messages)
            {
                var t = mapper.GetMappedTypeFor(m.GetType());

                var baseType = t.BaseType;
                while (baseType != typeof(object) && baseType != null)
                {
                    if (MessageConventionExtensions.IsMessageType(baseType))
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
                    if (MessageConventionExtensions.IsMessageType(i))
                    {
                        if (!result.Contains(i.FullName))
                        {
                            result.Add(i.FullName);
                        }
                    }
                }
            }

            return result;
        }
        
        const string BASETYPE = "baseType";

        Dictionary<Type, TypeMetaData> metaDatas = new Dictionary<Type, TypeMetaData>();

        [ThreadStatic]
        static string defaultNameSpace;

        /// <summary>
        /// Used for serialization
        /// </summary>
        [ThreadStatic]
        static IDictionary<string, string> namespacesToPrefix;

        /// <summary>
        /// Used for deserialization
        /// </summary>
        [ThreadStatic]
        static IDictionary<string, string> prefixesToNamespaces;

        [ThreadStatic]
        static List<Type> messageBaseTypes;

        [ThreadStatic]
        static List<Type> namespacesToAdd;

        static ILog logger = LogManager.GetLogger(typeof(XmlMessageSerializer));

        /// <summary>
        /// Initializes an instance of a <see cref="XmlMessageSerializer"/>.
        /// </summary>
        public XmlMessageSerializer(IMessageMapper mapper)
        {
            Namespace = "http://tempuri.net";
            this.mapper = mapper;
            GetTypeMetadata(typeof(EncryptedValue));
        }

        /// <summary>
        /// Initialized the serializer with the given message types
        /// </summary>
        public void Initialize(IEnumerable<Type> types)
        {
        }

    }
}
