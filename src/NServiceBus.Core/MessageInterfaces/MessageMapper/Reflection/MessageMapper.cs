namespace NServiceBus.MessageInterfaces.MessageMapper.Reflection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;
    using Logging;
    using Utils.Reflection;

    /// <summary>
    /// Uses reflection to map between interfaces and their generated concrete implementations.
    /// </summary>
    public class MessageMapper : IMessageMapper
    {
        /// <summary>
        /// Scans the given types generating concrete classes for interfaces.
        /// </summary>
        /// <param name="types"></param>
        public void Initialize(IEnumerable<Type> types)
        {
            var typesToList = types.ToList();

            if (types == null || !typesToList.Any())
                return;

            string name = typesToList.First().Namespace + SUFFIX;

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(name),
                AssemblyBuilderAccess.Run
                );

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(name);

            foreach (Type t in typesToList)
                InitType(t, moduleBuilder);
        }

        /// <summary>
        /// Generates a concrete implementation of the given type if it is an interface.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="moduleBuilder"></param>
        public void InitType(Type t, ModuleBuilder moduleBuilder)
        {
            if (t == null)
                return;

            if (t.IsSimpleType())
                return;

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                InitType(t.GetElementType(), moduleBuilder);

                foreach (var interfaceType in t.GetInterfaces())
                {
					foreach (var g in interfaceType.GetGenericArguments())
					{
						if(g == t)
							continue;

						InitType(g, moduleBuilder);
					}
                }

                return;
            }

            var typeName = GetTypeName(t);

            //already handled this type, prevent infinite recursion
            if (nameToType.ContainsKey(typeName))
                return;

            if (t.IsInterface)
            {
                GenerateImplementationFor(t, moduleBuilder);
            }
            else
                typeToConstructor[t] = t.GetConstructor(Type.EmptyTypes);

            nameToType[typeName] = t;

            foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                InitType(field.FieldType, moduleBuilder);

            foreach (PropertyInfo prop in t.GetProperties())
                InitType(prop.PropertyType, moduleBuilder);
        }

        void GenerateImplementationFor(Type interfaceType, ModuleBuilder moduleBuilder)
        {
            if (interfaceType.GetMethods().Any(mi => !(mi.IsSpecialName && (mi.Name.StartsWith("set_") || mi.Name.StartsWith("get_")))))
            {
                Logger.Warn(string.Format("Interface {0} contains methods and can there for not be mapped. Be aware that non mapped interface can't be used to send messages.",interfaceType.Name));
                return;
            }
            var mapped = CreateTypeFrom(interfaceType, moduleBuilder);
            interfaceToConcreteTypeMapping[interfaceType] = mapped;
            concreteToInterfaceTypeMapping[mapped] = interfaceType;
            typeToConstructor[mapped] = mapped.GetConstructor(Type.EmptyTypes);
        }

        private static string GetTypeName(Type t)
        {
            var args = t.GetGenericArguments();
            if (args.Length == 2)
                if (typeof(KeyValuePair<,>).MakeGenericType(args) == t)
                    return t.SerializationFriendlyName();

            return t.FullName;
        }

        /// <summary>
        /// Generates a new full name for a type to be generated for the given type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public string GetNewTypeName(Type t)
        {
            return t.FullName + SUFFIX;
        }

        /// <summary>
        /// Generates the concrete implementation of the given type.
        /// Only properties on the given type are generated in the concrete implementation.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="moduleBuilder"></param>
        /// <returns></returns>
        public Type CreateTypeFrom(Type t, ModuleBuilder moduleBuilder)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                GetNewTypeName(t),
                TypeAttributes.Serializable | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
                typeof(object)
                );

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            foreach (PropertyInfo prop in GetAllProperties(t))
            {
                Type propertyType = prop.PropertyType;

                FieldBuilder fieldBuilder = typeBuilder.DefineField(
                    "field_" + prop.Name,
                    propertyType,
                    FieldAttributes.Private);

                PropertyBuilder propBuilder = typeBuilder.DefineProperty(
                    prop.Name,
                    prop.Attributes | PropertyAttributes.HasDefault,
                    propertyType,
                    null);

                foreach (var customAttribute in prop.GetCustomAttributes(true))
                {
                    AddCustomAttributeToProperty(customAttribute, propBuilder);
                }

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

        /// <summary>
        /// Given a custom attribute and property builder, adds an instance of custom attribute
        /// to the property builder
        /// </summary>
        /// <param name="customAttribute"></param>
        /// <param name="propBuilder"></param>
        private void AddCustomAttributeToProperty(object customAttribute, PropertyBuilder propBuilder)
        {
            var customAttributeBuilder = BuildCustomAttribute(customAttribute);
            if (customAttributeBuilder != null)
                propBuilder.SetCustomAttribute(customAttributeBuilder);
        }

        private static CustomAttributeBuilder BuildCustomAttribute(object customAttribute)
        {
            ConstructorInfo longestCtor = null;
            // Get constructor with the largest number of parameters
            foreach (ConstructorInfo cInfo in customAttribute.GetType().GetConstructors().
                Where(cInfo => longestCtor == null || longestCtor.GetParameters().Length < cInfo.GetParameters().Length))
                longestCtor = cInfo;

            if (longestCtor == null)
                return null;

            // For each constructor parameter, get corresponding (by name similarity) property and get its value
            var args = new object[longestCtor.GetParameters().Length];
            int pos = 0;
            foreach (var consParamInfo in longestCtor.GetParameters())
            {
                var attrPropInfo = customAttribute.GetType().GetProperty(consParamInfo.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (attrPropInfo != null)
                    args[pos] = attrPropInfo.GetValue(customAttribute, null);
                else
                {
                    args[pos] = null;
                    var attrFieldInfo = customAttribute.GetType().GetField(consParamInfo.Name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (attrFieldInfo == null)
                    {
                        if (consParamInfo.ParameterType.IsValueType)
                            args[pos] = Activator.CreateInstance(consParamInfo.ParameterType);
                    }
                    else
                        args[pos] = attrFieldInfo.GetValue(customAttribute);
                }
                ++pos;
            }

            var propList = new List<PropertyInfo>();
            var propValueList = new List<object>();
            foreach (var attrPropInfo in customAttribute.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!attrPropInfo.CanWrite)
                    continue;
                object defaultValue = null;
                var defaultAttrs = attrPropInfo.GetCustomAttributes(typeof(DefaultValueAttribute), true);
                if (defaultAttrs.Length > 0)
                {
                    defaultValue = ((DefaultValueAttribute)defaultAttrs[0]).Value;
                }
                var value = attrPropInfo.GetValue(customAttribute, null);
                if (value == defaultValue)
                    continue;
                propList.Add(attrPropInfo);
                propValueList.Add(value);
            }
            return new CustomAttributeBuilder(longestCtor, args, propList.ToArray(), propValueList.ToArray());
        }
        /// <summary>
        /// Returns all properties on the given type, going up the inheritance
        /// hierarchy.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static IEnumerable<PropertyInfo> GetAllProperties(Type t)
        {
            var props = new List<PropertyInfo>(t.GetProperties());
            foreach (Type interfaceType in t.GetInterfaces())
                props.AddRange(GetAllProperties(interfaceType));

            var names = new List<string>(props.Count);
            var dups = new List<PropertyInfo>(props.Count);
            foreach (var p in props)
                if (names.Contains(p.Name))
                    dups.Add(p);
                else
                    names.Add(p.Name);

            foreach (var d in dups)
                props.Remove(d);

            return props;
        }

        /// <summary>
        /// If the given type is concrete, returns the interface it was generated to support.
        /// If the given type is an interface, returns the concrete class generated to implement it.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Type GetMappedTypeFor(Type t)
        {
            if (t.IsClass)
            {
                Type result;
                concreteToInterfaceTypeMapping.TryGetValue(t, out result);
                if (result != null)
                    return result;

                return t;
            }

            Type toReturn = null;
            interfaceToConcreteTypeMapping.TryGetValue(t, out toReturn);

            return toReturn;
        }

        /// <summary>
        /// Returns the type mapped to the given name.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public Type GetMappedTypeFor(string typeName)
        {
            string name = typeName;
            if (typeName.EndsWith(SUFFIX, StringComparison.Ordinal))
            {
                name = typeName.Substring(0, typeName.Length - SUFFIX.Length);
            }

            if (nameToType.ContainsKey(name))
                return nameToType[name];

            return Type.GetType(name);
        }

        /// <summary>
        /// Calls the generic CreateInstance and performs the given
        /// action on the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public T CreateInstance<T>(Action<T> action)
        {
            T result = CreateInstance<T>();

            if (action != null)
                action(result);

            return result;
        }

        /// <summary>
        /// Calls the non-generic CreateInstance and returns its result
        /// cast to T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T CreateInstance<T>()
        {
            return (T)CreateInstance(typeof(T));
        }

        /// <summary>
        /// If the given type is an interface, finds its generated concrete
        /// implementation, instantiates it, and returns the result.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public object CreateInstance(Type t)
        {
            Type mapped = t;
            if (t.IsInterface || t.IsAbstract)
            {
                mapped = GetMappedTypeFor(t);
                if (mapped == null)
                    throw new ArgumentException("Could not find a concrete type mapped to " + t.FullName);
            }

            ConstructorInfo constructor = null;
            typeToConstructor.TryGetValue(mapped, out constructor);
            if (constructor != null)
                return constructor.Invoke(null);

            return FormatterServices.GetUninitializedObject(mapped);
        }

        private static readonly string SUFFIX = "__impl";
        private static readonly Dictionary<Type, Type> interfaceToConcreteTypeMapping = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> concreteToInterfaceTypeMapping = new Dictionary<Type, Type>();
        private static readonly Dictionary<string, Type> nameToType = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, ConstructorInfo> typeToConstructor = new Dictionary<Type, ConstructorInfo>();
        private static ILog Logger = LogManager.GetLogger(typeof(MessageMapper));
    }
}
