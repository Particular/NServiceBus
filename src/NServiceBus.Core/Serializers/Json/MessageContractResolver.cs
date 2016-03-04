namespace NServiceBus
{
    using System;
    using MessageInterfaces;
    using Newtonsoft.Json.Serialization;

    class MessageContractResolver : DefaultContractResolver
    {
        public MessageContractResolver(IMessageMapper messageMapper)
        {
            this.messageMapper = messageMapper;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var mappedTypeFor = messageMapper.GetMappedTypeFor(objectType);

            if (mappedTypeFor == null)
            {
                return base.CreateObjectContract(objectType);
            }

            var jsonContract = base.CreateObjectContract(mappedTypeFor);
            jsonContract.DefaultCreator = () => messageMapper.CreateInstance(mappedTypeFor);

            return jsonContract;
        }

        IMessageMapper messageMapper;
    }
}