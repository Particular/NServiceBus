namespace NServiceBus
{
    using System;
    using System.Linq;

    //TODO: string comparisons
    //TODO: return false on the actual marker interface type
    /// <summary>
    /// Todo
    /// </summary>
    public class MarkerInterfaceConvention : IMessageConvention
    {
        /// <inheritdoc />
        public string Name => "NServiceBus default marker interfaces";

        /// <inheritdoc />
        public bool IsMessageType(Type type)
        {
            var intefaces = type.GetInterfaces();
            var isMessageType = intefaces.Any(i => i.FullName?.EndsWith(".IMessage") ?? false); // needs to be in a namespace (. check)
            if (!isMessageType)
            {
                isMessageType = intefaces.Any(i => (i.FullName?.EndsWith(".ICommand") ?? false) || (i.FullName?.EndsWith(".IEvent") ?? false)); // because users might not define the proper hierarchy
            }

            return isMessageType;
        }

        /// <inheritdoc />
        public bool IsCommandType(Type type) => type.GetInterfaces().Any(i => i.FullName?.EndsWith(".ICommand") ?? false);

        /// <inheritdoc />
        public bool IsEventType(Type type) => type.GetInterfaces().Any(i => i.FullName?.EndsWith(".IEvent") ?? false);
    }

    /// <summary>
    /// A message convention that uses the built-in NServiceBus marker interfaces.
    /// </summary>
    public class NServiceBusMarkerInterfaceConvention : IMessageConvention
    {
        /// <inheritdoc cref="IMessageConvention"/>
        public string Name => "NServiceBus Marker Interfaces";

        /// <inheritdoc cref="IMessageConvention"/>
        public bool IsCommandType(Type type)
        {
            Guard.AgainstNull(nameof(type), type);
            return typeof(ICommand).IsAssignableFrom(type) && typeof(ICommand) != type;
        }

        /// <inheritdoc cref="IMessageConvention"/>
        public bool IsEventType(Type type)
        {
            Guard.AgainstNull(nameof(type), type);
            return typeof(IEvent).IsAssignableFrom(type) && typeof(IEvent) != type;
        }

        /// <inheritdoc cref="IMessageConvention"/>
        public bool IsMessageType(Type type)
        {
            Guard.AgainstNull(nameof(type), type);
            return typeof(IMessage).IsAssignableFrom(type) &&
                 typeof(IMessage) != type &&
                 typeof(IEvent) != type &&
                 typeof(ICommand) != type;
        }
    }
}