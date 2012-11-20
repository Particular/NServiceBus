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
            var mappedType = _messageMapper.GetMappedTypeFor(serializedType) ?? serializedType;

            assemblyName = null;
            typeName = mappedType.AssemblyQualifiedName;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
          throw new NotImplementedException();
          //string resolvedTypeName = typeName + ", " + assemblyName;
          //return Type.GetType(resolvedTypeName, true);
        }
    }
}