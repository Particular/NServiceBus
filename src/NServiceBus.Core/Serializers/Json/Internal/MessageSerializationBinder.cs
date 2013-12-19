namespace NServiceBus.Serializers.Json.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using MessageInterfaces;

    public class MessageSerializationBinder : SerializationBinder
    {
        private readonly IMessageMapper _messageMapper;
        private readonly IList<Type> messageTypes;

        public MessageSerializationBinder(IMessageMapper messageMapper, IList<Type> messageTypes = null)
        {
            _messageMapper = messageMapper;
            this.messageTypes = messageTypes;
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var mappedType = _messageMapper.GetMappedTypeFor(serializedType) ?? serializedType;

            assemblyName = null;
            typeName = mappedType.AssemblyQualifiedName;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            Type resolved = null;
            if (messageTypes != null) // usually the requested message types are provided, so this should be fast
            {
                resolved = messageTypes.FirstOrDefault(t => t.Name.Contains(typeName));
            }
            if (resolved == null) // if the type has been used before it should be resolvable like this
            {
                resolved = Type.GetType(typeName);
            }
            if (resolved == null) // if the type has not been used before, we need to find it brute force
            {
                resolved = AppDomain.CurrentDomain.GetAssemblies()
                             .Select(a => a.GetType(typeName))
                             .FirstOrDefault(t => t != null); 
            }
            return resolved;
        }
    }
}