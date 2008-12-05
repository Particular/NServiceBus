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
            List<IMessage> result = new List<IMessage>();

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            string nameSpace = null;
            string fullNameSpace = null;
            foreach (XmlAttribute attr in doc.DocumentElement.Attributes)
            {
                if (attr.Name == "xmlns")
                    nameSpace = attr.Value + "/";
                if (attr.Name == "xmlns:extended")
                    fullNameSpace = attr.Value;
            }

            domainSpecificNameSpace = fullNameSpace.Replace(nameSpace, string.Empty);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                object m = null;
                Process(node, ref m);

                result.Add(m as IMessage);
            }

            domainSpecificNameSpace = null;

            return result.ToArray();
        }

        private void Process(XmlNode node, ref object parent)
        {
            Type t = MessageMapper.GetMappedTypeFor(domainSpecificNameSpace + "." + node.Name);
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
            StringBuilder builder = new StringBuilder();

            Type leadingType = this.MessageMapper.GetMappedTypeFor(messages[0].GetType());

            builder.AppendLine("<?xml version=\"1.0\" ?>");
            builder.AppendLine(
                "<Messages xmlns=\"" + nameSpace + "\" xmlns:extended=\"" + nameSpace + "/" + leadingType.Namespace + "\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                );

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
            foreach (PropertyInfo prop in typeToProperties[t])
            {
                WriteEntry(prop.Name, prop.PropertyType, prop.GetValue(obj, null), builder);
            }

        }

        public void WriteObject(string name, Type type, object value, StringBuilder builder)
        {
            builder.AppendFormat("<{0}>\n", name);

            Write(builder, type, value);

            builder.AppendFormat("</{0}>\n", name);
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

        #endregion

        #region members

        private static readonly string XMLPREFIX = "d1p1";
        private static readonly string XMLTYPE = XMLPREFIX + ":type";
        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> typeToProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private static readonly Dictionary<Type, Type> typesToCreateForArrays = new Dictionary<Type, Type>();

        [ThreadStatic]
        private static string domainSpecificNameSpace;
        #endregion
    }
}
