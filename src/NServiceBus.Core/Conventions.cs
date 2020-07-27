namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Message convention definitions.
    /// </summary>
    public class Conventions
    {
        /// <summary>
        /// Returns true if the given type is a message type.
        /// </summary>
        public bool IsMessageType(Type t)
        {
            Guard.AgainstNull(nameof(t), t);
            try
            {
                return MessagesConventionCache.ApplyConvention(t,
                    typeHandle =>
                    {
                        var type = Type.GetTypeFromHandle(typeHandle);

                        if (IsInSystemConventionList(type))
                        {
                            return true;
                        }
                        if (type.IsFromParticularAssembly())
                        {
                            return false;
                        }
                        return IsMessageTypeAction(type) ||
                               IsCommandTypeAction(type) ||
                               IsEventTypeAction(type);
                    });
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Message convention. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Returns true is message is a system message type.
        /// </summary>
        public bool IsInSystemConventionList(Type t)
        {
            Guard.AgainstNull(nameof(t), t);
            return IsSystemMessageActions.Any(isSystemMessageAction => isSystemMessageAction(t));
        }

        /// <summary>
        /// Add system message convention.
        /// </summary>
        /// <param name="definesMessageType">Function to define system message convention.</param>
        public void AddSystemMessagesConventions(Func<Type, bool> definesMessageType)
        {
            Guard.AgainstNull(nameof(definesMessageType), definesMessageType);
            if (!IsSystemMessageActions.Contains(definesMessageType))
            {
                IsSystemMessageActions.Add(definesMessageType);
                MessagesConventionCache.Reset();
            }
        }

        /// <summary>
        /// Returns true if the given type is a command type.
        /// </summary>
        public bool IsCommandType(Type t)
        {
            Guard.AgainstNull(nameof(t), t);
            try
            {
                return CommandsConventionCache.ApplyConvention(t, typeHandle =>
                {
                    var type = Type.GetTypeFromHandle(typeHandle);
                    if (type.IsFromParticularAssembly())
                    {
                        return false;
                    }
                    return IsCommandTypeAction(type);
                });
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Command convention. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Returns true if the given property should be send via the DataBus.
        /// </summary>
        public bool IsDataBusProperty(PropertyInfo property)
        {
            Guard.AgainstNull(nameof(property), property);
            try
            {
                return IsDataBusPropertyAction(property);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate DataBus Property convention. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Returns true if the given type is a event type.
        /// </summary>
        public bool IsEventType(Type t)
        {
            Guard.AgainstNull(nameof(t), t);
            try
            {
                return EventsConventionCache.ApplyConvention(t, typeHandle =>
                {
                    var type = Type.GetTypeFromHandle(typeHandle);
                    if (type.IsFromParticularAssembly())
                    {
                        return false;
                    }
                    return IsEventTypeAction(type);
                });
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Event convention. See inner exception for details.", ex);
            }
        }

        internal bool CustomMessageTypeConventionUsed { get; private set; }

        internal List<DataBusPropertyInfo> GetDataBusProperties(object message)
        {
            return cache.GetOrAdd(message.GetType(), messageType =>
            {
                var properties = new List<DataBusPropertyInfo>();
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var propertyInfo in messageType.GetProperties())
                {
                    if (IsDataBusProperty(propertyInfo))
                    {
                        properties.Add(new DataBusPropertyInfo
                        {
                            Name = propertyInfo.Name,
                            Getter = DelegateFactory.CreateGet(propertyInfo),
                            Setter = DelegateFactory.CreateSet(propertyInfo)
                        });
                    }
                }
                return properties;
            });
        }

        internal void DefineMessageTypeConvention(Func<Type, bool> definesMessageType)
        {
            IsMessageTypeAction = definesMessageType;
            CustomMessageTypeConventionUsed = true;
        }

        internal Func<Type, bool> IsCommandTypeAction = t => typeof(ICommand).IsAssignableFrom(t) && typeof(ICommand) != t;

        internal Func<PropertyInfo, bool> IsDataBusPropertyAction = p => typeof(IDataBusProperty).IsAssignableFrom(p.PropertyType) && typeof(IDataBusProperty) != p.PropertyType;

        internal Func<Type, bool> IsEventTypeAction = t => typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t;

        ConcurrentDictionary<Type, List<DataBusPropertyInfo>> cache = new ConcurrentDictionary<Type, List<DataBusPropertyInfo>>();

        ConventionCache CommandsConventionCache = new ConventionCache();
        ConventionCache EventsConventionCache = new ConventionCache();

        Func<Type, bool> IsMessageTypeAction = t => typeof(IMessage).IsAssignableFrom(t) &&
                                                    typeof(IMessage) != t &&
                                                    typeof(IEvent) != t &&
                                                    typeof(ICommand) != t;

        List<Func<Type, bool>> IsSystemMessageActions = new List<Func<Type, bool>>();
        ConventionCache MessagesConventionCache = new ConventionCache();

        class ConventionCache
        {
            public bool ApplyConvention(Type type, Func<RuntimeTypeHandle, bool> action)
            {
                return cache.GetOrAdd(type.TypeHandle, action);
            }

            public void Reset()
            {
                cache.Clear();
            }

            ConcurrentDictionary<RuntimeTypeHandle, bool> cache = new ConcurrentDictionary<RuntimeTypeHandle, bool>();
        }
    }
}