namespace NServiceBus.Serializers.Json.Internal
{
    using System;
    using MessageInterfaces;
    using Newtonsoft.Json.Serialization;

    class MessageContractResolver : DefaultContractResolver
    {
        IMessageMapper messageMapper;

        public MessageContractResolver(IMessageMapper messageMapper)
            : base(true)
        {
            this.messageMapper = messageMapper;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var mappedTypeFor = messageMapper.GetMappedTypeFor(objectType);

            if (mappedTypeFor == null)
                return base.CreateObjectContract(objectType);

            var jsonContract = base.CreateObjectContract(mappedTypeFor);
            jsonContract.DefaultCreator = () => messageMapper.CreateInstance(mappedTypeFor);

            return jsonContract;
        }
    }
}