namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    ///     Message related conventions.
    /// </summary>
    public class Conventions
    {
        /// <summary>
        ///     Returns true if the given object is a message.
        /// </summary>
        public bool IsMessage(object o)
        {
            return IsMessageType(o.GetType());
        }

        /// <summary>
        ///     Returns true if the given type is a message type.
        /// </summary>
        public bool IsMessageType(Type t)
        {
            try
            {
                return MessagesConventionCache.ApplyConvention(t,
                    type => IsMessageTypeAction(type) ||
                            IsCommandTypeAction(type) ||
                            IsEventTypeAction(type) ||
                            IsInSystemConventionList(type));
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Message convention. See inner exception for details.", ex);
            }
        }

        /// <summary>
        ///     Returns true is message is a system message type.
        /// </summary>
        public bool IsInSystemConventionList(Type t)
        {
            return IsSystemMessageActions.Any(isSystemMessageAction => isSystemMessageAction(t));
        }

        /// <summary>
        ///     Add system message convention
        /// </summary>
        /// <param name="definesMessageType">Function to define system message convention</param>
        public void AddSystemMessagesConventions(Func<Type, bool> definesMessageType)
        {
            if (!IsSystemMessageActions.Contains(definesMessageType))
            {
                IsSystemMessageActions.Add(definesMessageType);
            }
        }

        /// <summary>
        ///     Returns true if the given object is a command.
        /// </summary>
        public bool IsCommand(object o)
        {
            return IsCommandType(o.GetType());
        }

        /// <summary>
        ///     Returns true if the given type is a command type.
        /// </summary>
        public bool IsCommandType(Type t)
        {
            try
            {
                return CommandsConventionCache.ApplyConvention(t, type => IsCommandTypeAction(type));
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Command convention. See inner exception for details.", ex);
            }
        }

        /// <summary>
        ///     Returns true if the given message should not be written to disk when sent.
        /// </summary>
        public bool IsExpressMessage(object o)
        {
            return IsExpressMessageType(o.GetType());
        }

        /// <summary>
        ///     Returns true if the given type should not be written to disk when sent.
        /// </summary>
        public bool IsExpressMessageType(Type t)
        {
            try
            {
                return ExpressConventionCache.ApplyConvention(t, type => IsExpressMessageAction(type));
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Express convention. See inner exception for details.", ex);
            }
        }

        /// <summary>
        ///     Returns true if the given property should be encrypted
        /// </summary>
        public bool IsEncryptedProperty(PropertyInfo property)
        {
            try
            {
                //the message mutator will cache the whole message so we don't need to cache here
                return IsEncryptedPropertyAction(property);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Encrypted Property convention. See inner exception for details.", ex);
            }
        }

        /// <summary>
        ///     Returns true if the given property should be send via the DataBus
        /// </summary>
        public bool IsDataBusProperty(PropertyInfo property)
        {
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
        ///     Returns true if the given object is a event.
        /// </summary>
        public bool IsEvent(object o)
        {
            return IsEventType(o.GetType());
        }

        /// <summary>
        ///     Returns true if the given type is a event type.
        /// </summary>
        public bool IsEventType(Type t)
        {
            try
            {
                return EventsConventionCache.ApplyConvention(t, type => IsEventTypeAction(type));
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to evaluate Event convention. See inner exception for details.", ex);
            }
        }

        readonly ConventionCache CommandsConventionCache = new ConventionCache();
        readonly ConventionCache EventsConventionCache = new ConventionCache();
        readonly ConventionCache ExpressConventionCache = new ConventionCache();
        readonly ConventionCache MessagesConventionCache = new ConventionCache();

        /// <summary>
        ///     The function used to determine whether a type is a command type.
        /// </summary>
        public Func<Type, bool> IsCommandTypeAction = t => typeof(ICommand).IsAssignableFrom(t) && typeof(ICommand) != t;

        /// <summary>
        ///     The function used to determine whether a property should be treated as a databus property.
        /// </summary>
        public Func<PropertyInfo, bool> IsDataBusPropertyAction = property => typeof(IDataBusProperty).IsAssignableFrom(property.PropertyType) && typeof(IDataBusProperty) != property.PropertyType;

        /// <summary>
        ///     The function used to determine whether a property should be encrypted
        /// </summary>
        public Func<PropertyInfo, bool> IsEncryptedPropertyAction = property => typeof(WireEncryptedString).IsAssignableFrom(property.PropertyType);

        /// <summary>
        ///     The function used to determine whether a type is a event type.
        /// </summary>
        public Func<Type, bool> IsEventTypeAction = t => typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t;

        /// <summary>
        ///     The function used to determine if a type is an express message (the message should not be written to disk).
        /// </summary>
        public Func<Type, bool> IsExpressMessageAction = t => t.GetCustomAttributes(typeof(ExpressAttribute), true)
            .Any();

        /// <summary>
        ///     The function used to determine whether a type is a message type.
        /// </summary>
        public Func<Type, bool> IsMessageTypeAction = t => typeof(IMessage).IsAssignableFrom(t) &&
                                                           typeof(IMessage) != t &&
                                                           typeof(IEvent) != t &&
                                                           typeof(ICommand) != t;

        /// <summary>
        ///     Contains list of System messages' conventions
        /// </summary>
        public List<Func<Type, bool>> IsSystemMessageActions = new List<Func<Type, bool>>();

        /// <summary>
        ///     The function to evaluate whether the message has a time to be received or not ( <see cref="TimeSpan.MaxValue" />).
        /// </summary>
        public Func<Type, TimeSpan> TimeToBeReceivedAction = t =>
        {
            var attributes = t.GetCustomAttributes(typeof(TimeToBeReceivedAttribute), true)
                .Select(s => s as TimeToBeReceivedAttribute)
                .ToList();

            return attributes.Count > 0 ? attributes.Last().TimeToBeReceived : TimeSpan.MaxValue;
        };

        class ConventionCache
        {
            public bool ApplyConvention(Type type, Func<Type, bool> action)
            {
                bool result;

                if (cache.TryGetValue(type, out result))
                {
                    return result;
                }

                result = action(type);

                cache[type] = result;

                return result;
            }

            IDictionary<Type, bool> cache = new ConcurrentDictionary<Type, bool>();
        }
    }
}