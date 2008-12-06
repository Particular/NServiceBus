using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using NServiceBus.Serialization;
using NServiceBus.MessageInterfaces;

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

            IEnumerable<PropertyInfo> props = GetAllPropertiesForType(t);
            typeToProperties[t] = props;

            foreach (PropertyInfo prop in props)
                InitType(prop.PropertyType);
        }

        IEnumerable<PropertyInfo> GetAllPropertiesForType(Type t)
        {
            List<PropertyInfo> result = new List<PropertyInfo>(t.GetProperties());

            if (t.IsInterface)
                foreach (Type interfaceType in t.GetInterfaces())
                    result.AddRange(GetAllPropertiesForType(interfaceType));

            return result;
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

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                object m = null;
                Process(node, ref m);

                result.Add(m as IMessage);
            }

            defaultNameSpace = null;

            return result.ToArray();
        }

        private void Process(XmlNode node, ref object parent)
        {
            string typeName = defaultNameSpace + "." + node.Name;
            if (node.Name.Contains(":"))
            {
                int colonIndex = node.Name.IndexOf(":");
                string name = node.Name.Substring(colonIndex + 1);
                string prefix = node.Name.Substring(0, colonIndex);
                string nameSpace = prefixesToNamespaces[prefix];

                typeName = nameSpace.Substring(nameSpace.LastIndexOf("/") + 1) + "." + name;
            }

            Type t = MessageMapper.GetMappedTypeFor(typeName);
            if (t == null)
                return;

            parent = GetObjectOfTypeFromNode(t, node);
        }

        public object GetObjectOfTypeFromNode(Type t, XmlNode node)
        {
            object result = MessageMapper.CreateInstance(t);

            foreach (XmlNode n in node.ChildNodes)
            {
                PropertyInfo prop = GetProperty(t, n.Name);
                if (prop != null)
                {
                    object val = GetPropertyValue(prop.PropertyType, n);
                    if (val != null)
                        prop.SetValue(result, val, null);
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

        public object GetPropertyValue(Type type, XmlNode n)
        {
            if (n.ChildNodes.Count == 0)
                return null;

            if (n.ChildNodes.Count == 1 && n.ChildNodes[0] is XmlText)
            {
                if (type == typeof(string))
                    return n.ChildNodes[0].InnerText;

                if (type.IsPrimitive)
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
                    object newParent = null;
                    Process(xn, ref newParent);

                    if (list != null)
                        list.Add(newParent);
                }

                if (isArray)
                    return typeToCreate.GetMethod("ToArray").Invoke(list, null);
                else
                    return list;
            }

            return GetObjectOfTypeFromNode(type, n);
        }

        #endregion

        #region Serialize

        public void Serialize(IMessage[] messages, Stream stream)
        {
            namespacesToPrefix = new Dictionary<string, int>();

            StringBuilder builder = new StringBuilder();

            List<string> namespaces = GetNamespaces(messages, MessageMapper);

            builder.AppendLine("<?xml version=\"1.0\" ?>");
            
            builder.Append("<Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"");

            for (int i = 0; i < namespaces.Count; i++)
            {
                builder.AppendFormat(" xmlns{0}=\"{1}/{2}\"", (i == namespaces.Count - 1 ? ":q" + i : ""), nameSpace, namespaces[i]);
                namespacesToPrefix[namespaces[i]] = i;
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
        }

        public void WriteObject(string name, Type type, object value, StringBuilder builder)
        {
            string element = name;
            int i = namespacesToPrefix[type.Namespace];

            if (i > 0)
                element = "q" + i + ":" + name;

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
        private static readonly Dictionary<Type, Type> typesToCreateForArrays = new Dictionary<Type, Type>();

        [ThreadStatic]
        private static string defaultNameSpace;

        /// <summary>
        /// Used for serialization
        /// </summary>
        [ThreadStatic]
        private static IDictionary<string, int> namespacesToPrefix;

        /// <summary>
        /// Used for deserialization
        /// </summary>
        [ThreadStatic]
        private static IDictionary<string, string> prefixesToNamespaces;

        #endregion
    }
}
