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
        readonly IMessageMapper mapper;
        IList<Type> messageTypes;

        /// <summary>
        /// The namespace to place in outgoing XML.
        /// </summary>
        public string Namespace
        {
            get { return nameSpace; }
            set { nameSpace = value; }
        }

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

        /// <summary>
        /// Scans the given type storing maps to fields and properties to save on reflection at runtime.
        /// </summary>
        /// <param name="t"></param>
        public void InitType(Type t)
        {
            logger.Debug("Initializing type: " + t.AssemblyQualifiedName);

            if (t.IsSimpleType())
                return;

            if (typeof(XContainer).IsAssignableFrom(t))
            {
                typesBeingInitialized.Add(t);

                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                if (t.IsArray)
                    typesToCreateForArrays[t] = typeof(List<>).MakeGenericType(t.GetElementType());


                foreach (Type g in t.GetGenericArguments())
                    InitType(g);

                //Handle dictionaries - initialize relevant KeyValuePair<T,K> types.
                foreach (Type interfaceType in t.GetInterfaces())
                {
                    Type[] arr = interfaceType.GetGenericArguments();
                    if (arr.Length == 1)
                        if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(t))
                            InitType(arr[0]);
                }

                if (t.IsGenericType && t.IsInterface) //handle IEnumerable<Something>
                {
                    var g = t.GetGenericArguments();
                    var e = typeof(IEnumerable<>).MakeGenericType(g);

                    if (e.IsAssignableFrom(t))
                        typesToCreateForEnumerables[t] = typeof(List<>).MakeGenericType(g);
                }

                if (t.IsGenericType && t.GetGenericArguments().Length == 1)
                {
                    Type setType = typeof(ISet<>).MakeGenericType(t.GetGenericArguments());

                    if (setType.IsAssignableFrom(t)) //handle ISet<Something>
                    {
                        var g = t.GetGenericArguments();
                        var e = typeof(IEnumerable<>).MakeGenericType(g);

                        if (e.IsAssignableFrom(t))
                            typesToCreateForEnumerables[t] = typeof(List<>).MakeGenericType(g);
                    }
                }

                return;
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
                    InitType(args[0]);

                    if (!args[0].GetGenericArguments().Any())
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
                if (!prop.CanWrite && !isKeyValuePair)
                {
                    continue;
                }

                if (prop.GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Length > 0)
                {
                    continue;
                }

                if (typeof(IList) == prop.PropertyType)
                    throw new NotSupportedException("IList is not a supported property type for serialization, use List instead. Type: " + t.FullName + " Property: " + prop.Name);

                var args = prop.PropertyType.GetGenericArguments();

                if (args.Length == 1)
                {
                    if (typeof(IList<>).MakeGenericType(args) == prop.PropertyType)
                        throw new NotSupportedException("IList<T> is not a supported property type for serialization, use List<T> instead. Type: " + t.FullName + " Property: " + prop.Name);
                    if (typeof(ISet<>).MakeGenericType(args) == prop.PropertyType)
                        throw new NotSupportedException("ISet<T> is not a supported property type for serialization, use HashSet<T> instead. Type: " + t.FullName + " Property: " + prop.Name);
                }

                if (args.Length == 2)
                {
                    if (typeof(IDictionary<,>).MakeGenericType(args) == prop.PropertyType)
                        throw new NotSupportedException("IDictionary<T, K> is not a supported property type for serialization, use Dictionary<T,K> instead. Type: " + t.FullName + " Property: " + prop.Name + ". Consider using a concrete Dictionary<T, K> instead, where T and K cannot be of type 'System.Object'");

                    if (args[0].FullName == "System.Object" || args[1].FullName == "System.Object")
                        throw new NotSupportedException("Dictionary<T, K> is not a supported when Key or Value is of Type System.Object. Type: " + t.FullName + " Property: " + prop.Name + ". Consider using a concrete Dictionary<T, K> where T and K are not of type 'System.Object'");
                }

                result.Add(prop);
            }

            if (t.IsInterface)
                foreach (Type interfaceType in t.GetInterfaces())
                    result.AddRange(GetAllPropertiesForType(interfaceType, false));

            return result.Distinct();
        }

        IEnumerable<FieldInfo> GetAllFieldsForType(Type t)
        {
            return t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
        }

        #region Deserialize

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

            XmlReader reader = SanitizeInput
                                  ? XmlReader.Create(new XmlSanitizingStream(stream), new XmlReaderSettings { CheckCharacters = false })
                                  : XmlReader.Create(stream, new XmlReaderSettings { CheckCharacters = false });

            doc.Load(reader);

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
                            Type baseType = mapper.GetMappedTypeFor(attr.Value);
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
                Type nodeType = null;

                if (messageTypesToDeserialize != null)
                    nodeType = messageTypesToDeserialize.FirstOrDefault();

                object m = Process(doc.DocumentElement, null, nodeType);

                if (m == null)
                    throw new SerializationException("Could not deserialize message.");

                result.Add(m);
            }
            else
            {
                int position = 0;
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Whitespace)
                        continue;


                    Type nodeType = null;

                    if (messageTypesToDeserialize != null && position < messageTypesToDeserialize.Count)
                        nodeType = messageTypesToDeserialize.ElementAt(position);


                    var m = Process(node, null, nodeType);

                    result.Add(m);

                    position++;
                }
            }

            defaultNameSpace = null;

            return result.ToArray();
        }

        private object Process(XmlNode node, object parent, Type nodeType = null)
        {
            if (nodeType == null)
                nodeType = InferNodeType(node, parent);

            return GetObjectOfTypeFromNode(nodeType, node);
        }

        Type InferNodeType(XmlNode node, object parent)
        {
            string name = node.Name;
            string typeName = name;

            if (!string.IsNullOrEmpty(defaultNameSpace))
                typeName = defaultNameSpace + "." + typeName;

            if (name.Contains(":"))
            {
                int colonIndex = node.Name.IndexOf(":");
                name = name.Substring(colonIndex + 1);
                string prefix = node.Name.Substring(0, colonIndex);
                string ns = prefixesToNamespaces[prefix];

                typeName = ns.Substring(ns.LastIndexOf("/") + 1) + "." + name;
            }

            if (name.Contains("NServiceBus."))
                typeName = name;


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
                        return args[0];
                }

                var prop = parent.GetType().GetProperty(name);

                if (prop != null)
                    return prop.PropertyType;
            }

            var mappedType = mapper.GetMappedTypeFor(typeName);

            if (mappedType != null)
                return mappedType;


            logger.Debug("Could not load " + typeName + ". Trying base types...");
            foreach (Type baseType in messageBaseTypes)
                try
                {
                    logger.Debug("Trying to deserialize message to " + baseType.FullName);
                    return baseType;
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch
                {
                } // intentionally swallow exception
            // ReSharper restore EmptyGeneralCatchClause

            throw new TypeLoadException("Could not determine type for node: '" + node.Name + "'.");
        }

        private object GetObjectOfTypeFromNode(Type t, XmlNode node)
        {
            if (t.IsSimpleType() || t == typeof(Uri))
                return GetPropertyValue(t, node);

            if (typeof(IEnumerable).IsAssignableFrom(t))
                return GetPropertyValue(t, node);

            object result = mapper.CreateInstance(t);

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
            if ((n.ChildNodes.Count == 1) && (n.ChildNodes[0] is XmlCharacterData))
            {
                var text = n.ChildNodes[0].InnerText;

                var args = type.GetGenericArguments();
                if (args.Length == 1 && args[0].IsValueType)
                {
                    if (args[0].GetGenericArguments().Any())
                        return GetPropertyValue(args[0], n);

                    var nullableType = typeof(Nullable<>).MakeGenericType(args);
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

                if (!typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (n.ChildNodes[0] is XmlWhitespace)
                        return Activator.CreateInstance(type);

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
                bool isArray = type.IsArray;

                bool isISet = false;
                if (type.IsGenericType && type.GetGenericArguments().Length == 1)
                {
                    Type setType = typeof(ISet<>).MakeGenericType(type.GetGenericArguments());
                    isISet = setType.IsAssignableFrom(type);
                }

                Type typeToCreate = type;
                if (isArray)
                    typeToCreate = typesToCreateForArrays[type];

                if (typesToCreateForEnumerables.ContainsKey(type)) //handle IEnumerable<Something>
                    typeToCreate = typesToCreateForEnumerables[type];

                if (typeof(IList).IsAssignableFrom(typeToCreate))
                {
                    var list = Activator.CreateInstance(typeToCreate) as IList;
                    if (list != null)
                    {
                        foreach (XmlNode xn in n.ChildNodes)
                        {
                            if (xn.NodeType == XmlNodeType.Whitespace)
                                continue;

                            object m = Process(xn, list);
                            list.Add(m);
                        }

                        if (isArray)
                            return typeToCreate.GetMethod("ToArray").Invoke(list, null);

                        if (isISet)
                            return Activator.CreateInstance(type, typeToCreate.GetMethod("ToArray").Invoke(list, null));
                    }

                    return list;
                }
            }

            if (n.ChildNodes.Count == 0)
                if (type == typeof(string))
                    return string.Empty;
                else
                    return null;


            return GetObjectOfTypeFromNode(type, n);
        }

        #endregion

        #region Serialize

        /// <summary>
        /// Serializes the given messages to the given stream.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="stream"></param>
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
                List<string> baseTypes = GetBaseTypes(messages);

                WrapMessages(builder, namespaces, baseTypes, messageBuilder);
            }
            byte[] buffer = Encoding.UTF8.GetBytes(builder.ToString());
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
            for (int i = 0; i < namespaces.Count; i++)
            {
                string prefix = "q" + i;
                if (i == 0)
                    prefix = "";

                if (namespaces[i] != null)
                    namespacesToPrefix[namespaces[i]] = prefix;
            }
            return namespaces;
        }

        private void Write(StringBuilder builder, Type t, object obj)
        {
            if (obj == null)
                return;

            if (!typeToProperties.ContainsKey(t))
                throw new InvalidOperationException(string.Format("Type {0} was not registered in the serializer. Check that it appears in the list of configured assemblies/types to scan.", t.FullName));

            foreach (PropertyInfo prop in typeToProperties[t])
            {
                if (IsIndexedProperty(prop))
                {
                    throw new NotSupportedException(string.Format("Type {0} contains an indexed property named {1}. Indexed properties are not supported on message types.", t.FullName, prop.Name));
                }
                WriteEntry(prop.Name, prop.PropertyType, propertyInfoToLateBoundProperty[prop].Invoke(obj), builder);
            }

            foreach (FieldInfo field in typeToFields[t])
                WriteEntry(field.Name, field.FieldType, fieldInfoToLateBoundField[field].Invoke(obj), builder);
        }

        static bool IsIndexedProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo != null)
            {
                return propertyInfo.GetIndexParameters().Length > 0;
            }

            return false;
        }

        private void WriteObject(string name, Type type, object value, StringBuilder builder, bool useNS = false)
        {
            string element = name;
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
                    FormatAsString(value));

                return;
            }

            if (!string.IsNullOrEmpty(prefix))
                element = prefix + ":" + name;

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

        private void CreateStartElementWithNamespaces(List<string> namespaces, List<string> baseTypes, StringBuilder builder, string element)
        {
            string prefix;

            builder.AppendFormat(
                "<{0} xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"",
                element);

            for (int i = 0; i < namespaces.Count; i++)
            {
                prefix = "q" + i;
                if (i == 0)
                    prefix = "";

                builder.AppendFormat(" xmlns{0}=\"{1}/{2}\"", (prefix != "" ? ":" + prefix : prefix), nameSpace,
                                     namespaces[i]);
            }

            foreach (var t in namespacesToAdd)
                builder.AppendFormat(" xmlns:{0}=\"{1}\"", t.Name.ToLower(), t.Name);

            for (int i = 0; i < baseTypes.Count; i++)
            {
                prefix = BASETYPE;
                if (i != 0)
                    prefix += i;

                builder.AppendFormat(" xmlns:{0}=\"{1}\"", prefix, baseTypes[i]);
            }

            builder.Append(">\n");
        }

        private void WriteEntry(string name, Type type, object value, StringBuilder builder)
        {
            if (value == null)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                    return;

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

                    Type[] interfaces = type.GetInterfaces();
                    if (type.IsInterface) interfaces = interfaces.Union(new[] { type }).ToArray();

                    //Get generic type from list: T for List<T>, KeyValuePair<T,K> for IDictionary<T,K>
                    foreach (Type interfaceType in interfaces)
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
                        if (obj != null && obj.GetType().IsSimpleType())
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
                return XmlConvert.ToString((bool)value);
            if (value is byte)
                return XmlConvert.ToString((byte)value);
            if (value is char)
                return Escape((char)value);
            if (value is double)
                return XmlConvert.ToString((double)value);
            if (value is ulong)
                return XmlConvert.ToString((ulong)value);
            if (value is uint)
                return XmlConvert.ToString((uint)value);
            if (value is ushort)
                return XmlConvert.ToString((ushort)value);
            if (value is long)
                return XmlConvert.ToString((long)value);
            if (value is int)
                return XmlConvert.ToString((int)value);
            if (value is short)
                return XmlConvert.ToString((short)value);
            if (value is sbyte)
                return XmlConvert.ToString((sbyte)value);
            if (value is decimal)
                return XmlConvert.ToString((decimal)value);
            if (value is float)
                return XmlConvert.ToString((float)value);
            if (value is Guid)
                return XmlConvert.ToString((Guid)value);
            if (value is DateTime)
                return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
            if (value is DateTimeOffset)
                return XmlConvert.ToString((DateTimeOffset)value);
            if (value is TimeSpan)
                return XmlConvert.ToString((TimeSpan)value);
            if (value is string)
                return Escape(value as string);

            return value.ToString();
        }

        private static string Escape(char c)
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
                return String.Format("&#x{0:X};", (int)c);
            }

            //Should not get here but just in case!
            return c.ToString();
        }

        private static string Escape(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            StringBuilder builder = null; // initialize if we need it

            int startIndex = 0;
            for (var i = 0; i < str.Length; ++i)
            {
                char c = str[i];
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
                            builder = new StringBuilder(str.Length + ss.Length);
                        }
                        if (startIndex < i)
                        {
                            builder.Append(str, startIndex, i - startIndex);
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
                        builder = new StringBuilder(str.Length + 8);
                    }
                    if (startIndex < i)
                    {
                        builder.Append(str, startIndex, i - startIndex);
                    }
                    startIndex = i + 1;
                    builder.AppendFormat("&#x{0:X};", (int) c);
                }
            }

            if (startIndex < str.Length)
            {
                if (builder == null)
                {
                    return str;
                }
                builder.Append(str, startIndex, str.Length - startIndex);
            }

            if (builder != null)
            {
                return builder.ToString();
            }

            //Should not get here but just in case!
            return str;
        }

        List<string> GetNamespaces(object[] messages)
        {
            var result = new List<string>();

            foreach (var m in messages)
            {
                string ns = mapper.GetMappedTypeFor(m.GetType()).Namespace;
                if (!result.Contains(ns))
                    result.Add(ns);
            }

            return result;
        }

        List<string> GetBaseTypes(object[] messages)
        {
            var result = new List<string>();

            foreach (var m in messages)
            {
                Type t = mapper.GetMappedTypeFor(m.GetType());

                Type baseType = t.BaseType;
                while (baseType != typeof(object) && baseType != null)
                {
                    if (MessageConventionExtensions.IsMessageType(baseType))
                        if (!result.Contains(baseType.FullName))
                            result.Add(baseType.FullName);

                    baseType = baseType.BaseType;
                }

                foreach (Type i in t.GetInterfaces())
                    if (MessageConventionExtensions.IsMessageType(i))
                        if (!result.Contains(i.FullName))
                            result.Add(i.FullName);
            }

            return result;
        }

        #endregion

        #region members

        private const string BASETYPE = "baseType";

        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> typeToProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly Dictionary<Type, IEnumerable<FieldInfo>> typeToFields = new Dictionary<Type, IEnumerable<FieldInfo>>();
        private static readonly Dictionary<Type, Type> typesToCreateForArrays = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> typesToCreateForEnumerables = new Dictionary<Type, Type>();
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

        private static readonly ILog logger = LogManager.GetLogger(typeof(XmlMessageSerializer));

        /// <summary>
        /// Initializes an instace of a NServiceBus.Serializers.XML.XmlMessageSerializer
        /// </summary>
        /// <param name="mapper">Message Mapper</param>
        public XmlMessageSerializer(IMessageMapper mapper)
        {
            this.mapper = mapper;
        }

        #endregion

        /// <summary>
        /// Initialized the serializer with the given message types
        /// </summary>
        /// <param name="types"></param>
        public void Initialize(IEnumerable<Type> types)
        {
            messageTypes = types.ToList();

            if (!messageTypes.Contains(typeof(EncryptedValue)))
                messageTypes.Add(typeof(EncryptedValue));

            foreach (Type t in messageTypes)
                InitType(t);

        }

        string nameSpace = "http://tempuri.net";
    }
}
