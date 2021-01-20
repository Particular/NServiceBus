namespace NServiceBus
{
    using System;

    class OverridableMessageConvention : IMessageConvention
    {
        readonly IMessageConvention inner;
        Func<Type, bool> isCommandType;
        Func<Type, bool> isEventType;
        Func<Type, bool> isMessageType;

        public OverridableMessageConvention(IMessageConvention inner)
        {
            this.inner = inner;
        }

        public string Name => ConventionModified ? $"Modified {inner.Name}" : inner.Name;

        public bool IsCommandType(Type type) => isCommandType?.Invoke(type) ?? inner.IsCommandType(type);

        public bool IsEventType(Type type) => isEventType?.Invoke(type) ?? inner.IsEventType(type);

        public bool IsMessageType(Type type) => isMessageType?.Invoke(type) ?? inner.IsMessageType(type);

        public void DefiningCommandsAs(Func<Type, bool> isCommandType) => this.isCommandType = isCommandType;

        public void DefiningEventsAs(Func<Type, bool> isEventType) => this.isEventType = isEventType;

        public void DefiningMessagesAs(Func<Type, bool> isMessageType) => this.isMessageType = isMessageType;

        public bool ConventionModified => isCommandType != null || isEventType != null || isMessageType != null;
    }
}