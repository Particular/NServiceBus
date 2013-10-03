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
        ConcreteTypeBuilder concreteTypeBuilder;
        Dictionary<Type, MetaData> messageTypeToMetaDatas = new Dictionary<Type, MetaData>();
        Dictionary<string, MetaData> messageNameToMetaDatas = new Dictionary<string, MetaData>();
        Dictionary<Type, MetaData> concreteToInterfaceTypeMapping = new Dictionary<Type, MetaData>();

        public MessageMapper()
        {
            concreteTypeBuilder = new ConcreteTypeBuilder();
        }

        /// <summary>
        /// Scans the given types generating concrete classes for interfaces.
        /// </summary>
        public void Initialize(IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                InitType(t);
            }
        }


        /// <summary>
        /// Generates a concrete implementation of the given type if it is an interface.
        /// </summary>
        MetaData InitType(Type type)
        {

            if (type == null)
            {
                return null;
            }

            if (type.IsSimpleType())
            {
                return null;
            }

            MetaData metaData;
            if (ConcreteTypeBuilder.IsProxy(type))
            {
                if (concreteToInterfaceTypeMapping.TryGetValue(type, out metaData))
                {
                    return metaData;
                }
            }

            if (messageTypeToMetaDatas.TryGetValue(type, out metaData))
            {
                return metaData;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                InitType(type.GetElementType());

                foreach (var interfaceType in type.GetInterfaces())
                {
					foreach (var g in interfaceType.GetGenericArguments())
					{
					    if (g == type)
					    {
					        continue;
					    }

						InitType(g);
					}
                }

            }

            metaData = new MetaData
                       {
                           MessageType = type,
                           TypeFullName = type.GetTypeName()
                       };

            if (type.IsInterface)
            {
                if (type.GetMethods().Any(mi => !(mi.IsSpecialName && (mi.Name.StartsWith("set_") || mi.Name.StartsWith("get_")))))
                {
                    Logger.Warn(string.Format("Interface {0} contains methods and can there for not be mapped. Be aware that non mapped interface can't be used to send messages.", type.Name));
                    return null;
                }
                var concreteType = concreteTypeBuilder.GenerateConcreteImplementation(type);
                metaData.ConcreteType = concreteType;
                concreteToInterfaceTypeMapping[concreteType] = metaData;
                var constructorInfo = concreteType.GetConstructor(Type.EmptyTypes);
                metaData.ConstructInstance = () => constructorInfo.Invoke(null);
            }
            else
            {
                metaData.ConcreteType = type;
                var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                if (constructorInfo == null)
                {
                    metaData.ConstructInstance = () => FormatterServices.GetUninitializedObject(type);
                }
                else
                {
                    metaData.ConstructInstance = () => constructorInfo.Invoke(null);
                }
                concreteToInterfaceTypeMapping[type] = metaData;
            }

            messageTypeToMetaDatas[type] = metaData;
            messageNameToMetaDatas[metaData.TypeFullName] = metaData;

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                InitType(field.FieldType);
            }

            foreach (var prop in type.GetProperties())
            {
                InitType(prop.PropertyType);
            }
            return metaData;
        }

        public Type GetMessageType(Type concreteType)
        {
            var metaData = InitType(concreteType);
            if (metaData != null)
            {
                return metaData.MessageType;
            }
            return null;
        }

        public Type GetConcreteType(Type messageType)
        {
            var metaData = InitType(messageType);
            if (metaData != null)
            {
                return metaData.ConcreteType;
            }
            return null;
        }

        /// <summary>
        /// Returns the type mapped to the given name.
        /// </summary>
        public Type GetMappedTypeFor(string typeName)
        {
            typeName = ConcreteTypeBuilder.GetUnProxiedName(typeName);
            
            MetaData metaData;
            if (messageNameToMetaDatas.TryGetValue(typeName, out metaData))
            {
                return metaData.ConcreteType;
            }

            return Type.GetType(typeName);
        }

        /// <summary>
        /// Calls the generic <see cref="CreateInstance{T}()"/> and performs the given action on the result.
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
            var metaData = InitType(t);
            if (metaData == null)
            {
                throw new ArgumentException("Could not find a concrete type mapped to " + t.FullName);
            }
            return metaData.ConstructInstance();
        }

        static ILog Logger = LogManager.GetLogger(typeof(MessageMapper));
    }
}
