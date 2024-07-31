﻿namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logging;

/// <summary>
/// Message convention definitions.
/// </summary>
public class Conventions
{
    /// <summary>
    /// Initializes message conventions with the default NServiceBus conventions.
    /// </summary>
    public Conventions()
    {
        defaultMessageConvention = new OverridableMessageConvention(new NServiceBusMarkerInterfaceConvention());
        conventions.Add(defaultMessageConvention);
    }

    /// <summary>
    /// Returns true if the given type is a message type.
    /// </summary>
    public bool IsMessageType(Type t)
    {
        ArgumentNullException.ThrowIfNull(t);
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

                    foreach (var convention in conventions)
                    {
                        if (convention.IsMessageType(type))
                        {
                            if (logger.IsDebugEnabled)
                            {
                                logger.Debug($"{type.FullName} identified as message type by {convention.Name} convention.");
                            }
                            return true;
                        }

                        if (convention.IsCommandType(type))
                        {
                            if (logger.IsDebugEnabled)
                            {
                                logger.Debug($"{type.FullName} identified as command type by {convention.Name} but does not match message type convention. Treating as a message.");
                            }
                            return true;
                        }

                        if (convention.IsEventType(type))
                        {
                            if (logger.IsDebugEnabled)
                            {
                                logger.Debug($"{type.FullName} identified as event type by {convention.Name} but does not match message type convention. Treating as a message.");
                            }
                            return true;
                        }
                    }
                    return false;
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
        ArgumentNullException.ThrowIfNull(t);
        return IsSystemMessageActions.Any(isSystemMessageAction => isSystemMessageAction(t));
    }

    /// <summary>
    /// Add system message convention.
    /// </summary>
    /// <param name="definesMessageType">Function to define system message convention.</param>
    public void AddSystemMessagesConventions(Func<Type, bool> definesMessageType)
    {
        ArgumentNullException.ThrowIfNull(definesMessageType);
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
        ArgumentNullException.ThrowIfNull(t);
        try
        {
            return CommandsConventionCache.ApplyConvention(t, typeHandle =>
            {
                var type = Type.GetTypeFromHandle(typeHandle);
                if (type.IsFromParticularAssembly())
                {
                    return false;
                }

                foreach (var convention in conventions)
                {
                    if (convention.IsCommandType(type))
                    {
                        if (logger.IsDebugEnabled)
                        {
                            logger.Debug($"{type.FullName} identified as command type by {convention.Name} convention.");
                        }
                        return true;
                    }

                }
                return false;
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
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public bool IsDataBusProperty(PropertyInfo property)
    {
        return databusConventions.IsDataBusProperty(property);
    }

    /// <summary>
    /// Returns true if the given type is a event type.
    /// </summary>
    public bool IsEventType(Type t)
    {
        ArgumentNullException.ThrowIfNull(t);
        try
        {
            return EventsConventionCache.ApplyConvention(t, typeHandle =>
            {
                var type = Type.GetTypeFromHandle(typeHandle);
                if (type.IsFromParticularAssembly())
                {
                    return false;
                }

                foreach (var convention in conventions)
                {
                    if (convention.IsEventType(type))
                    {
                        if (logger.IsDebugEnabled)
                        {
                            logger.Debug($"{type.FullName} identified as event type by {convention.Name} convention.");
                        }
                        return true;
                    }
                }

                return false;
            });
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to evaluate Event convention. See inner exception for details.", ex);
        }
    }

    internal bool CustomMessageTypeConventionUsed => defaultMessageConvention.ConventionModified || conventions.Count > 1;

    internal string[] RegisteredConventions => conventions.Select(x => x.Name).ToArray();

    internal void DefineMessageTypeConvention(Func<Type, bool> definesMessageType)
    {
        defaultMessageConvention.DefiningMessagesAs(definesMessageType);
    }

    internal void DefineCommandTypeConventions(Func<Type, bool> definesCommandType)
    {
        defaultMessageConvention.DefiningCommandsAs(definesCommandType);
    }

    internal void DefineEventTypeConventions(Func<Type, bool> definesEventType)
    {
        defaultMessageConvention.DefiningEventsAs(definesEventType);
    }

    internal void Add(IMessageConvention messageConvention)
    {
        conventions.Add(messageConvention);
    }

    readonly ConventionCache CommandsConventionCache = new ConventionCache();
    readonly ConventionCache EventsConventionCache = new ConventionCache();

    readonly List<Func<Type, bool>> IsSystemMessageActions = [];
    readonly ConventionCache MessagesConventionCache = new ConventionCache();

    readonly List<IMessageConvention> conventions = [];
    readonly OverridableMessageConvention defaultMessageConvention;
    internal DataBusConventions databusConventions;

    static readonly ILog logger = LogManager.GetLogger<Conventions>();

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

        readonly ConcurrentDictionary<RuntimeTypeHandle, bool> cache = new ConcurrentDictionary<RuntimeTypeHandle, bool>();
    }
}