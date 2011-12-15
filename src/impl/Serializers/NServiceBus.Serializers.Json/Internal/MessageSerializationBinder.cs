using System;
using System.Runtime.Serialization;
using NServiceBus.MessageInterfaces;

namespace NServiceBus.Serializers.Json.Internal
{
    public class MessageSerializationBinder : SerializationBinder
    {
        private readonly IMessageMapper _messageMapper;

        public MessageSerializationBinder(IMessageMapper messageMapper)
        {
            _messageMapper = messageMapper;
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var mappedType = _messageMapper.GetMappedTypeFor(serializedType);
            assemblyName = mappedType.Assembly.GetName().Name;
            typeName = mappedType.FullName;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            string resolvedTypeName = typeName + ", " + assemblyName;

            return Type.GetType(resolvedTypeName, true);
        }
    }
}