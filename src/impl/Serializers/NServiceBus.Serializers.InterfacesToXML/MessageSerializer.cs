using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using NServiceBus.Serialization;

namespace NServiceBus.Serializers.InterfacesToXML
{
    public class MessageSerializer : IMessageSerializer
    {
        #region Initialize

        public void Initialize(params Type[] types)
        {
            if (types == null || types.Length == 0)
                return;

            string name = types[0].Namespace + SUFFIX;

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(name),
                AssemblyBuilderAccess.RunAndSave
                );

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(name, name + ".dll");

            foreach (Type t in types)
            {
                if (t.IsInterface)
                {
                    Type mapped = CreateTypeFrom(t, moduleBuilder);
                    TypeMapping[t] = mapped;
                }

                nameToType[t.FullName] = t;
            }

            assemblyBuilder.Save(name + ".dll");
        }

        public Type CreateTypeFrom(Type t, ModuleBuilder moduleBuilder)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                t.Namespace + SUFFIX + "." + t.Name,
                TypeAttributes.Serializable | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
                typeof(object)
                );

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            foreach (PropertyInfo prop in t.GetProperties())
            {
                Type propertyType = prop.PropertyType;
                nameToType[propertyType.FullName] = propertyType;

                FieldBuilder fieldBuilder = typeBuilder.DefineField(
                    "field_" + prop.Name,
                    propertyType,
                    FieldAttributes.Private);

                PropertyBuilder propBuilder = typeBuilder.DefineProperty(
                    prop.Name,
                    prop.Attributes | PropertyAttributes.HasDefault,
                    propertyType,
                    null);

                MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(
                    "get_" + prop.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask,
                    propertyType,
                    Type.EmptyTypes);

                ILGenerator getIL = getMethodBuilder.GetILGenerator();
                // For an instance property, argument zero is the instance. Load the 
                // instance, then load the private field and return, leaving the
                // field value on the stack.
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getIL.Emit(OpCodes.Ret);

                // Define the "set" accessor method for Number, which has no return
                // type and takes one argument of type int (Int32).
                MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(
                    "set_" + prop.Name,
                    getMethodBuilder.Attributes,
                    null,
                    new Type[] { propertyType });

                ILGenerator setIL = setMethodBuilder.GetILGenerator();
                // Load the instance and then the numeric argument, then store the
                // argument in the field.
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Stfld, fieldBuilder);
                setIL.Emit(OpCodes.Ret);

                // Last, map the "get" and "set" accessor methods to the 
                // PropertyBuilder. The property is now complete. 
                propBuilder.SetGetMethod(getMethodBuilder);
                propBuilder.SetSetMethod(setMethodBuilder);
            }

            typeBuilder.AddInterfaceImplementation(t);

            return typeBuilder.CreateType();
        }

        #endregion

        #region Deserialize

        public IMessage[] Deserialize(Stream stream)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            List<IMessage> result = new List<IMessage>();

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                object m = null;
                Process(node, ref m);

                result.Add(m as IMessage);
            }

            return result.ToArray();
        }

        private void Process(XmlNode node, ref object parent)
        {
            if (node.Attributes.Count > 0)
            {
                XmlAttribute attribute = node.Attributes[XMLTYPE];
                if (attribute != null)
                {
                    Type t = Type.GetType(attribute.Value);
                    if (t == null)
                    {
                        if (nameToType.ContainsKey(attribute.Value))
                            t = nameToType[attribute.Value];
                    }

                    if (t != null)
                    {
                        parent = CreateImplementationOf(t);

                        foreach (PropertyInfo prop in parent.GetType().GetProperties())
                        {
                            foreach (XmlNode n in node.ChildNodes)
                            {
                                if (n.Name == prop.Name)
                                {
                                    if ((prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string)) && n.ChildNodes.Count == 1 && n.ChildNodes[0] is XmlText)
                                    {
                                        prop.SetValue(parent,
                                                      Convert.ChangeType(n.ChildNodes[0].InnerText, prop.PropertyType),
                                                      null);
                                        break;
                                    }

                                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                                    {
                                        prop.SetValue(parent, Activator.CreateInstance(prop.PropertyType), null);

                                        IList list = prop.GetValue(parent, null) as IList;

                                        foreach (XmlNode xn in n.ChildNodes)
                                        {
                                            object newParent = null;
                                            Process(xn, ref newParent);

                                            if (list != null)
                                                list.Add(newParent);
                                        }

                                        break;
                                    }
                                    else
                                    {
                                        object newParent = null;
                                        Process(n, ref newParent);

                                        prop.SetValue(parent, newParent, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Serialize

        public void Serialize(IMessage[] messages, Stream stream)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" ?>");
            builder.AppendLine(
                "<Messages " + XMLTYPE + "=\"ArrayOfAnyType\" xmlns:" + XMLPREFIX + "=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                );

            foreach (IMessage m in messages)
            {
                Type t = m.GetType();
                if (t.Namespace.EndsWith(SUFFIX))
                {
                    string s = t.Namespace.Replace(SUFFIX, "") + "." + t.Name;
                    t = nameToType[s];
                }

                WriteObject("m", t, m, this.GetType().GetMethod("Write").MakeGenericMethod(t),
                            builder);
            }

            builder.AppendLine("</Messages>");

            byte[] buffer = UnicodeEncoding.UTF8.GetBytes(builder.ToString());
            stream.Write(buffer, 0, buffer.Length);
        }

        public void Write<T>(StringBuilder builder, T o)
        {
            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
                {
                    builder.AppendFormat("<{0}>{1}</{0}>\n", prop.Name, prop.GetValue(o, null));
                    continue;
                }

                if (prop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    builder.AppendFormat("<{0} " + XMLTYPE + "=\"ArrayOfAnyType\">\n", prop.Name);

                    Type baseType = typeof(object);
                    Type[] generics = prop.PropertyType.GetGenericArguments();
                    if (generics != null)
                        baseType = generics[0];

                    MethodInfo toCall = this.GetType().GetMethod("Write").MakeGenericMethod(baseType);

                    foreach (object obj in ((IEnumerable)prop.GetValue(o, null)))
                        WriteObject("e", baseType, obj, toCall, builder);

                    builder.AppendFormat("</{0}>\n", prop.Name);
                    continue;
                }
                else
                {
                    WriteObject(prop.Name, prop.PropertyType, prop.GetValue(o, null), this.GetType().GetMethod("Write").MakeGenericMethod(prop.PropertyType), builder);
                }
            }
        }

        public void WriteObject(string name, Type type, object value, MethodInfo call, StringBuilder builder)
        {
            builder.AppendFormat("<{0} " + XMLTYPE + "=\"{1}\">\n", name, type);

            call.Invoke(this, new object[] { builder, value });

            builder.AppendFormat("</{0}>\n", name);
        }

        #endregion

        #region CreateImplementationOf<T>

        public T CreateImplementationOf<T>()
        {
            return (T)CreateImplementationOf(typeof(T));
        }

        public object CreateImplementationOf(Type t)
        {
            return Activator.CreateInstance(GetMappedTypeFor(t));
        }

        public Type GetMappedTypeFor(Type t)
        {
            if (t.IsClass && !t.IsAbstract)
                return t;

            if (TypeMapping.ContainsKey(t))
                return TypeMapping[t];

            return null;
        }

        #endregion

        #region members

        private static readonly Dictionary<Type, Type> TypeMapping = new Dictionary<Type, Type>();
        private static readonly Dictionary<string, Type> nameToType = new Dictionary<string, Type>();
        private static readonly string SUFFIX = ".__Impl";
        private static readonly string XMLPREFIX = "d1p1";
        private static readonly string XMLTYPE = XMLPREFIX + ":type";

        #endregion
    }
}
