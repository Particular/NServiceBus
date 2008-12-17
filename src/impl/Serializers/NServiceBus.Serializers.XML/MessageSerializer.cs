using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using NServiceBus.Serialization;
using NServiceBus.MessageInterfaces;
using System.Runtime.Serialization;

namespace NServiceBus.Serializers.XML
{
    public class MessageSerializer : IMessageSerializer
    {
        public virtual IMessageMapper MessageMapper { get; set; }

        private string nameSpace = "http://tempuri.net";
        public virtual string Namespace
        {
            get { return nameSpace; }
            set { nameSpace = value; }
        }

        public virtual List<Type> AdditionalTypes { get; set; }

        public void Initialize(params Type[] types)
        {
            if (AdditionalTypes == null)
                AdditionalTypes = new List<Type>();

            AdditionalTypes.AddRange(types);
            this.MessageMapper.Initialize(AdditionalTypes.ToArray());

            foreach (Type t in AdditionalTypes)
                InitType(t);
        }

        public void InitType(Type t)
        {
            if (t.IsPrimitive || t == typeof(string) || t == typeof(Guid) || t == typeof(DateTime))
                return;

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                if (t.IsArray)
                    typesToCreateForArrays[t] = typeof(List<>).MakeGenericType(t.GetElementType());

                foreach (Type g in t.GetGenericArguments())
                    InitType(g);

                return;
            }

            var props = GetAllPropertiesForType(t);
            typeToProperties[t] = props;
            var fields = GetAllFieldsForType(t);
            typeToFields[t] = fields;

            foreach (PropertyInfo prop in props)
                InitType(prop.PropertyType);

            foreach (FieldInfo field in fields)
                InitType(field.FieldType);
        }

        IEnumerable<PropertyInfo> GetAllPropertiesForType(Type t)
        {
            List<PropertyInfo> result = new List<PropertyInfo>(t.GetProperties());

            if (t.IsInterface)
                foreach (Type interfaceType in t.GetInterfaces())
                    result.AddRange(GetAllPropertiesForType(interfaceType));

            return result;
        }

        IEnumerable<FieldInfo> GetAllFieldsForType(Type t)
        {
            return t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
        }

        #region Deserialize

        public IMessage[] Deserialize(Stream stream)
        {
            prefixesToNamespaces = new Dictionary<string, string>();
            List<IMessage> result = new List<IMessage>();

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

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
                string nameSpace = prefixesToNamespaces[prefix];

                typeName = nameSpace.Substring(nameSpace.LastIndexOf("/") + 1) + "." + name;
            }

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
                throw new TypeLoadException("Could not load type '" + typeName + "'.");

            return GetObjectOfTypeFromNode(t, node);
        }

        public object GetObjectOfTypeFromNode(Type t, XmlNode node)
        {
            object result = MessageMapper.CreateInstance(t);

            foreach (XmlNode n in node.ChildNodes)
            {
                PropertyInfo prop = GetProperty(t, n.Name);
                if (prop != null)
                {
                    object val = GetPropertyValue(prop.PropertyType, n, result);
                    if (val != null)
                        prop.SetValue(result, val, null);
                }

                FieldInfo field = GetField(t, n.Name);
                if (field != null)
                {
                    object val = GetPropertyValue(field.FieldType, n, result);
                    if (val != null)
                        field.SetValue(result, val);
                }
            }

            return result;
        }

        public PropertyInfo GetProperty(Type t, string name)
        {
            IEnumerable<PropertyInfo> props;
            typeToProperties.TryGetValue(t, out props);

            if (props == null)
                return null;

            foreach (PropertyInfo prop in props)
                if (prop.Name == name)
                    return prop;

            return null;
        }

        public FieldInfo GetField(Type t, string name)
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

        public object GetPropertyValue(Type type, XmlNode n, object parent)
        {
            if (n.ChildNodes.Count == 1 && n.ChildNodes[0] is XmlText)
            {
                if (type == typeof(string))
                    return n.ChildNodes[0].InnerText;

                if (type.IsPrimitive || type == typeof(decimal))
                    return Convert.ChangeType(n.ChildNodes[0].InnerText, type);

                if (type == typeof (Guid))
                    return new Guid(n.ChildNodes[0].InnerText);

                if (type == typeof(DateTime))
                    return XmlConvert.ToDateTime(n.ChildNodes[0].InnerText, XmlDateTimeSerializationMode.Utc);

                if (type == typeof(TimeSpan))
                    return XmlConvert.ToTimeSpan(n.ChildNodes[0].InnerText);

                if (type.IsEnum)
                    return Enum.Parse(type, n.ChildNodes[0].InnerText);
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                bool isArray = type.IsArray;

                Type typeToCreate = type;
                if (isArray)
                    typeToCreate = typesToCreateForArrays[type];

                IList list = Activator.CreateInstance(typeToCreate) as IList;

                foreach (XmlNode xn in n.ChildNodes)
                {
                    object m = Process(xn, list);

                    if (list != null)
                        list.Add(m);
                }

                if (isArray)
                    return typeToCreate.GetMethod("ToArray").Invoke(list, null);
                else
                    return list;
            }

            if (n.ChildNodes.Count == 0)
                return null;


            return GetObjectOfTypeFromNode(type, n);
        }

        #endregion

        #region Serialize

        public void Serialize(IMessage[] messages, Stream stream)
        {
            namespacesToPrefix = new Dictionary<string, string>();

            StringBuilder builder = new StringBuilder();

            List<string> namespaces = GetNamespaces(messages, MessageMapper);

            builder.AppendLine("<?xml version=\"1.0\" ?>");
            
            builder.Append("<Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"");

            for (int i = 0; i < namespaces.Count; i++)
            {
                string prefix = "q" + i;
                if (i == 0)
                    prefix = "";

                builder.AppendFormat(" xmlns{0}=\"{1}/{2}\"", (prefix != "" ? ":" + prefix : prefix), nameSpace, namespaces[i]);
                namespacesToPrefix[namespaces[i]] = prefix;
            }

            builder.Append(">\n");

            foreach (IMessage m in messages)
            {
                Type t = MessageMapper.GetMappedTypeFor(m.GetType());

                WriteObject(t.Name, t, m, builder);
            }

            builder.AppendLine("</Messages>");

            byte[] buffer = UnicodeEncoding.UTF8.GetBytes(builder.ToString());
            stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(StringBuilder builder, Type t, object obj)
        {
            if (obj == null)
                return;

            foreach (PropertyInfo prop in typeToProperties[t])
                WriteEntry(prop.Name, prop.PropertyType, prop.GetValue(obj, null), builder);

            foreach(FieldInfo field in typeToFields[t])
                WriteEntry(field.Name, field.FieldType, field.GetValue(obj), builder);
        }

        public void WriteObject(string name, Type type, object value, StringBuilder builder)
        {
            string element = name;
            string prefix = null;
            namespacesToPrefix.TryGetValue(type.Namespace, out prefix);

            if (prefix != null && prefix != "")
                element = prefix + ":" + name;

            builder.AppendFormat("<{0}>\n", element);

            Write(builder, type, value);

            builder.AppendFormat("</{0}>\n", element);
        }

        public void WriteEntry(string name, Type type, object value, StringBuilder builder)
        {
            if (type.IsValueType || type == typeof(string))
            {
                builder.AppendFormat("<{0}>{1}</{0}>\n", name, FormatAsString(value));
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                builder.AppendFormat("<{0}>\n", name);

                Type baseType = typeof(object);
                Type[] generics = type.GetGenericArguments();
                if (generics != null && generics.Length > 0)
                    baseType = generics[0];

                if (value != null)
                    foreach (object obj in ((IEnumerable)value))
                        WriteObject(baseType.Name, baseType, obj, builder);

                builder.AppendFormat("</{0}>\n", name);
                return;
            }

            WriteObject(name, type, value, builder);
        }

        private string FormatAsString(object value)
        {
            if (value == null)
                return string.Empty;
            if (value is bool)
                return value.ToString().ToLower();
            if (value is string)
                return value as string;
            if (value is DateTime)
                return ((DateTime) value).ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
            if (value is TimeSpan)
            {
                TimeSpan ts = (TimeSpan) value;
                return string.Format("P0Y0M{0}DT{1}H{2}M{3}S", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
            }
            if (value is Guid)
                return ((Guid) value).ToString();

            return value.ToString();
        }

        private static List<string> GetNamespaces(IMessage[] messages, IMessageMapper mapper)
        {
            List<string> result = new List<string>();

            foreach (IMessage m in messages)
            {
                string ns = mapper.GetMappedTypeFor(m.GetType()).Namespace;
                if (!result.Contains(ns))
                    result.Add(ns);
            }

            return result;
        }

        #endregion

        #region members

        private static readonly string XMLPREFIX = "d1p1";
        private static readonly string XMLTYPE = XMLPREFIX + ":type";
        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> typeToProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly Dictionary<Type, IEnumerable<FieldInfo>> typeToFields = new Dictionary<Type, IEnumerable<FieldInfo>>();
        private static readonly Dictionary<Type, Type> typesToCreateForArrays = new Dictionary<Type, Type>();

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

        #endregion
    }
}
