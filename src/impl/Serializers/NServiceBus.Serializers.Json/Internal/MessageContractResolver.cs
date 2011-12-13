using System;
using NServiceBus.MessageInterfaces;
using Newtonsoft.Json.Serialization;

namespace NServiceBus.Serializers.Json.Internal
{
    public class MessageContractResolver : DefaultContractResolver
    {
        private readonly IMessageMapper _messageMapper;

        public MessageContractResolver(IMessageMapper messageMapper)
            : base(true)
        {
            _messageMapper = messageMapper;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            if (!typeof(IMessage).IsAssignableFrom(objectType))
                return base.CreateObjectContract(objectType);

            Type mappedTypeFor = _messageMapper.GetMappedTypeFor(objectType);

            if (mappedTypeFor == null)
                return base.CreateObjectContract(objectType);

            var jsonContract = base.CreateObjectContract(mappedTypeFor);
            jsonContract.DefaultCreator = () => _messageMapper.CreateInstance(mappedTypeFor);

            return jsonContract;
        }
    }
}