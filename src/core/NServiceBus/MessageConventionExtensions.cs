namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Extension methods for message related conventions
    /// </summary>
    public static class MessageConventionExtensions
    {
        /// <summary>
        /// Returns true if the given object is a message.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsMessage(this object o)
        {
            return o.GetType().IsMessageType();
        }

        /// <summary>
        /// Returns true if the given type is a message type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsMessageType(this Type t)
        {
            return MessagesConventionCache.ApplyConvention(t,type => IsMessageTypeAction(type) || IsCommandTypeAction(type) || IsEventTypeAction(type));
        }

        /// <summary>
        /// Returns true if the given object is a command.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsCommand(this object o)
        {
            return o.GetType().IsCommandType();
        }

        /// <summary>
        /// Returns true if the given type is a command type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsCommandType(this Type t)
        {
            return CommandsConventionCache.ApplyConvention(t, type => IsCommandTypeAction(type));
        }


        /// <summary>
        /// Returns true if the given property should be encrypted
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool IsEncryptedProperty(this PropertyInfo property)
        {
            //the message mutator will cache the whole message so we don't need to cache here
            return IsEncryptedPropertyAction(property);
        }

        /// <summary>
        /// Returns true if the given object is a event.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsEvent(this object o)
        {
            return o.GetType().IsEventType();
        }

        /// <summary>
        /// Returns true if the given type is a event type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsEventType(this Type t)
        {
            return EventsConventionCache.ApplyConvention(t, type => IsEventTypeAction(type));
        }
        /// <summary>
        /// The function used to determine whether a type is a message type.
        /// </summary>
        public static Func<Type, bool> IsMessageTypeAction = t => typeof(IMessage).IsAssignableFrom(t) &&
                                                                    typeof(IMessage) != t &&
                                                                    typeof(IEvent) != t &&
                                                                    typeof(ICommand) != t;


        /// <summary>
        /// The function used to determine whether a type is a command type.
        /// </summary>
        public static Func<Type, bool> IsCommandTypeAction = t => typeof(ICommand).IsAssignableFrom(t) && typeof(ICommand) != t;


        /// <summary>
        /// The function used to determine whether a type is a event type.
        /// </summary>
        public static Func<Type, bool> IsEventTypeAction = t => typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t;

        /// <summary>
        /// The function used to determine whether a property should be encrypted
        /// </summary>
        public static Func<PropertyInfo, bool> IsEncryptedPropertyAction = property => typeof(WireEncryptedString).IsAssignableFrom(property.PropertyType);



        static readonly ConventionCache<Type> MessagesConventionCache = new ConventionCache<Type>();
        static readonly ConventionCache<Type> CommandsConventionCache = new ConventionCache<Type>();
        static readonly ConventionCache<Type> EventsConventionCache = new ConventionCache<Type>();

    }

    class ConventionCache<T>
    {
        readonly IDictionary<T,bool> cache = new ConcurrentDictionary<T,bool>();

        public bool ApplyConvention(T type,Func<T, bool> action)
        {
            bool result;

            if(cache.TryGetValue(type,out result))
                return result;

            result = action(type);

            cache[type] = result;

            return result;
        }
    }
}