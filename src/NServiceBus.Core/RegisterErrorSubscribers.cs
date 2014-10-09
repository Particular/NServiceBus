namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public static class RegisterErrorSubscribers
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        public static void RegisterErrorSubscriber<T>(this BusConfiguration config) where T : IErrorSubscriber
        {
            var subscribers = config.Settings.GetOrDefault<List<Type>>("ErrorSubscribers") ?? new List<Type>();

            subscribers.Add(typeof(T));

            config.Settings.Set("ErrorSubscribers", subscribers);
        }
    }
}