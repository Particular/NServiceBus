namespace NServiceBus.Serializers.Json.Internal
{
    using System;
    using System.Runtime.Serialization;
    using MessageInterfaces;

    public class MessageSerializationBinder : SerializationBinder
    {
        private readonly IMessageMapper _messageMapper;

        public MessageSerializationBinder(IMessageMapper messageMapper)
        {
            _messageMapper = messageMapper;
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            Type mappedType;
            //TODO: should not need null check
            if (_messageMapper.GetMessageType(serializedType) != null)
            {
                mappedType = _messageMapper.GetMessageType(serializedType);
            }
            else
            {
                mappedType = serializedType;
            }

            assemblyName = null;
            typeName = mappedType.AssemblyQualifiedName;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
          throw new NotImplementedException();
        }
    }
}