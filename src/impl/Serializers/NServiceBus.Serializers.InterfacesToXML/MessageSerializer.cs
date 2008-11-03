using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using NServiceBus.Serialization;
using NServiceBus.MessageInterfaces;

namespace NServiceBus.Serializers.InterfacesToXML
{
    public class MessageSerializer : IMessageSerializer
    {
        private IMessageMapper messageMapper;
        public IMessageMapper MessageMapper
        {
            get { return messageMapper; }
            set { messageMapper = value; }
        }

        private string nameSpace = "http://tempuri.net";
        public string Namespace
        {
            get { return nameSpace; }
            set { nameSpace = value; }
        }

        public void Initialize(params Type[] types)
        {
            this.messageMapper.Initialize(types);

            foreach (Type t in types)
                InitType(t);
        }

        public void InitType(Type t)
        {
            if (t.IsPrimitive || t == typeof(string) || t == typeof(Guid) || t == typeof(DateTime))
                return;

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                foreach (Type g in t.GetGenericArguments())
                    InitType(g);

                return;
            }

            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            typeToFields[t] = fields;

            PropertyInfo[] props = t.GetProperties();
            typeToProperties[t] = props;

            foreach(FieldInfo field in fields)
                InitType(field.FieldType);

            foreach (PropertyInfo prop in props)
                InitType(prop.PropertyType);
        }

        #region Deserialize

        public IMessage[] Deserialize(Stream stream)
        {
            List<IMessage> result = new List<IMessage>();

            //XmlReaderSettings settings = new XmlReaderSettings();
            //settings.IgnoreWhitespace = true;
            //settings.IgnoreProcessingInstructions = true;
            //settings.IgnoreComments = true;

            //XmlReader reader = XmlReader.Create(stream, settings);
            //reader.ReadStartElement();

            //while(!reader.EOF)
            //    result.Add(Process(reader) as IMessage);


            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                object m = null;
                Process(node, ref m);

                result.Add(m as IMessage);
            }

            //reader.Close();
            return result.ToArray();
        }

        private object Process(XmlReader reader)
        {
            object result = null;

            if (reader.HasAttributes)
            {
                string attr = reader.GetAttribute(XMLTYPE);
                Type t = messageMapper.GetMappedTypeFor(attr);
                if (t != null)
                    result = messageMapper.CreateInstance(t);

                if (result != null)
                {
                    reader.Read();
                    FillProperties(result, reader);
                }
            }

            return result;
        }

        private void FillProperties(object result, XmlReader reader)
        {
            
        }

        private void Process(XmlNode node, ref object parent)
        {
            if (node.Attributes.Count == 0)
                return;

            XmlAttribute attribute = node.Attributes[XMLTYPE];
            if (attribute == null)
                return;

            Type t = messageMapper.GetMappedTypeFor(attribute.Value);
            if (t == null)
                return;

            parent = messageMapper.CreateInstance(t);

            foreach (XmlNode n in node.ChildNodes)
            {
                PropertyInfo prop = GetProperty(t, n.Name);
                if (prop != null)
                {
                    object result = GetPropertyOrFieldValue(prop.PropertyType, n);
                    if (result != null)
                        prop.SetValue(parent, result, null);
                }
                else
                {
                    FieldInfo field = GetField(t, n.Name);
                    if (field != null)
                    {
                        object result = GetPropertyOrFieldValue(field.FieldType, n);
                        if (result != null)
                            field.SetValue(parent, result);
                    }
                }
            }
        }

        public PropertyInfo GetProperty(Type t, string name)
        {
            PropertyInfo[] props;
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
            FieldInfo[] fields;
            typeToFields.TryGetValue(t, out fields);

            if (fields == null)
                return null;

            foreach (FieldInfo field in fields)
                if (field.Name == name)
                    return field;

            return null;
        }

        public object GetPropertyOrFieldValue(Type type, XmlNode n)
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
                    return DateTime.Parse(n.ChildNodes[0].InnerText);

                if (type == typeof(TimeSpan))
                    return TimeSpan.Parse(n.ChildNodes[0].InnerText);

                if (type.IsEnum)
                    return Enum.Parse(type, n.ChildNodes[0].InnerText);
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                IList list = Activator.CreateInstance(type) as IList;

                foreach (XmlNode xn in n.ChildNodes)
                {
                    object newParent = null;
                    Process(xn, ref newParent);

                    if (list != null)
                        list.Add(newParent);
                }

                return list;
            }

            object result = null;
            Process(n, ref result);

            return result;
        }

        #endregion

        #region Serialize

        public void Serialize(IMessage[] messages, Stream stream)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" ?>");
            builder.AppendLine(
                "<Messages " + XMLTYPE + "=\"ArrayOfAnyType\" xmlns:" + XMLPREFIX + "=\"" + nameSpace + "\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                );

            foreach (IMessage m in messages)
            {
                Type t = messageMapper.GetMappedTypeFor(m.GetType());

                WriteObject("m", t, m, builder);
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

            foreach (FieldInfo field in typeToFields[t])
            {
                WriteEntry(field.Name, field.FieldType, field.GetValue(obj), builder);
            }

        }

        public void WriteObject(string name, Type type, object value, StringBuilder builder)
        {
            builder.AppendFormat("<{0} " + XMLTYPE + "=\"{1}\">\n", name, type);

            Write(builder, type, value);

            builder.AppendFormat("</{0}>\n", name);
        }

        public void WriteEntry(string name, Type type, object value, StringBuilder builder)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(TimeSpan) || type.IsEnum)
            {
                builder.AppendFormat("<{0}>{1}</{0}>\n", name, value);
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                builder.AppendFormat("<{0} " + XMLTYPE + "=\"ArrayOfAnyType\">\n", name);

                Type baseType = typeof(object);
                Type[] generics = type.GetGenericArguments();
                if (generics != null)
                    baseType = generics[0];

                foreach (object obj in ((IEnumerable)value))
                    WriteObject("e", baseType, obj, builder);

                builder.AppendFormat("</{0}>\n", name);
                return;
            }

            WriteObject(name, type, value, builder);
        }

        #endregion

        #region members

        private static readonly string XMLPREFIX = "d1p1";
        private static readonly string XMLTYPE = XMLPREFIX + ":type";
        private static readonly Dictionary<Type, FieldInfo[]> typeToFields = new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<Type, PropertyInfo[]> typeToProperties = new Dictionary<Type, PropertyInfo[]>();

        #endregion
    }
}
