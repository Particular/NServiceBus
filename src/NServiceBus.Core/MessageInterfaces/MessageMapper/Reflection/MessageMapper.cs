namespace NServiceBus.MessageInterfaces.MessageMapper.Reflection
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Logging;
    using Utils.Reflection;

    /// <summary>
    /// Uses reflection to map between interfaces and their generated concrete implementations.
    /// </summary>
    public class MessageMapper : IMessageMapper
    {

        ConcreteProxyCreator concreteProxyCreator;

        /// <summary>
        /// Initializes a new instance of <see cref="MessageMapper"/>.
        /// </summary>
        public MessageMapper()
        {
            concreteProxyCreator = new ConcreteProxyCreator();
        }

        /// <summary>
        /// Scans the given types generating concrete classes for interfaces.
        /// </summary>
        public void Initialize(IEnumerable<Type> types)
        {
            if (types == null)
            {
                return;
            }

            foreach (var t in types)
            {
                InitType(t);
            }
        }

        /// <summary>
        /// Generates a concrete implementation of the given type if it is an interface.
        /// </summary>
        void InitType(Type t)
        {
            if (t == null)
            {
                return;
            }

            if (t.IsSimpleType() || t.IsGenericTypeDefinition)
            {
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                InitType(t.GetElementType());

                foreach (var interfaceType in t.GetInterfaces())
                {
					foreach (var g in interfaceType.GetGenericArguments())
					{
						if(g == t)
							continue;

						InitType(g);
					}
                }

                return;
            }

            var typeName = GetTypeName(t);

            //already handled this type, prevent infinite recursion
            if (nameToType.ContainsKey(typeName))
            {
                return;
            }

            if (t.IsInterface)
            {
                GenerateImplementationFor(t);
            }
            else
            {
                typeToConstructor[t] = t.GetConstructor(Type.EmptyTypes);
            }

            nameToType[typeName] = t;

            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                InitType(field.FieldType);
            }

            foreach (var prop in t.GetProperties())
            {
                InitType(prop.PropertyType);
            }
        }

        void GenerateImplementationFor(Type interfaceType)
        {
            if (!interfaceType.IsVisible)
            {
                throw new Exception(string.Format("We can only generate a concrete implementation for '{0}' if '{0}' is public.", interfaceType));
            }

            if (interfaceType.GetMethods().Any(mi => !(mi.IsSpecialName && (mi.Name.StartsWith("set_") || mi.Name.StartsWith("get_")))))
            {
                Logger.Warn(string.Format("Interface {0} contains methods and can there for not be mapped. Be aware that non mapped interface can't be used to send messages.",interfaceType.Name));
                return;
            }

            var mapped = concreteProxyCreator.CreateTypeFrom(interfaceType);
            interfaceToConcreteTypeMapping[interfaceType] = mapped;
            concreteToInterfaceTypeMapping[mapped] = interfaceType;
            typeToConstructor[mapped] = mapped.GetConstructor(Type.EmptyTypes);
        }

        static string GetTypeName(Type t)
        {
            var args = t.GetGenericArguments();
            if (args.Length == 2)
            {
                if (typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]) == t)
                {
                    return t.SerializationFriendlyName();
                }
            }

            return t.FullName;
        }

        /// <summary>
        /// If the given type is concrete, returns the interface it was generated to support.
        /// If the given type is an interface, returns the concrete class generated to implement it.
        /// </summary>
        public Type GetMappedTypeFor(Type t)
        {
            if (t.IsClass)
            {
                Type result;
                concreteToInterfaceTypeMapping.TryGetValue(t, out result);
                if (result != null || t.IsGenericTypeDefinition)
                {
                    return result;
                }

                return t;
            }

            Type toReturn;
            interfaceToConcreteTypeMapping.TryGetValue(t, out toReturn);
            return toReturn;
        }

        /// <summary>
        /// Returns the type mapped to the given name.
        /// </summary>
        public Type GetMappedTypeFor(string typeName)
        {
            var name = typeName;
            if (typeName.EndsWith(ConcreteProxyCreator.SUFFIX, StringComparison.Ordinal))
            {
                name = typeName.Substring(0, typeName.Length - ConcreteProxyCreator.SUFFIX.Length);
            }

            Type type;
            if (nameToType.TryGetValue(name, out type))
            {
                return type;
            }

            return Type.GetType(name);
        }

        /// <summary>
        /// Calls the generic CreateInstance and performs the given action on the result.
        /// </summary>
        public T CreateInstance<T>(Action<T> action)
        {
            var result = CreateInstance<T>();

            if (action != null)
            {
                action(result);
            }

            return result;
        }

        /// <summary>
        /// Calls the <see cref="CreateInstance(Type)"/> and returns its result cast to <typeparamref name="T"/>.
        /// </summary>
        public T CreateInstance<T>()
        {
            return (T)CreateInstance(typeof(T));
        }

        /// <summary>
        /// If the given type is an interface, finds its generated concrete implementation, instantiates it, and returns the result.
        /// </summary>
        public object CreateInstance(Type t)
        {
            var mapped = t;
            if (t.IsInterface || t.IsAbstract)
            {
                mapped = GetMappedTypeFor(t);
                if (mapped == null)
                {
                    throw new ArgumentException("Could not find a concrete type mapped to " + t.FullName);
                }
            }

            ConstructorInfo constructor;
            typeToConstructor.TryGetValue(mapped, out constructor);
            if (constructor != null)
            {
                return constructor.Invoke(null);
            }

            return FormatterServices.GetUninitializedObject(mapped);
        }

        Dictionary<Type, Type> interfaceToConcreteTypeMapping = new Dictionary<Type, Type>();
        Dictionary<Type, Type> concreteToInterfaceTypeMapping = new Dictionary<Type, Type>();
        Dictionary<string, Type> nameToType = new Dictionary<string, Type>();
        Dictionary<Type, ConstructorInfo> typeToConstructor = new Dictionary<Type, ConstructorInfo>();
        static ILog Logger = LogManager.GetLogger<MessageMapper>();
    }
}
