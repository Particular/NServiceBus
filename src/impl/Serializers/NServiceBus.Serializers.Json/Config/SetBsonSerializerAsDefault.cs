using NServiceBus.ObjectBuilder;
using NServiceBus.Serialization;

namespace NServiceBus
{
	class SetBsonSerializerAsDefault : INeedInitialization
	{
		internal static bool UseBsonSerializer;

		void INeedInitialization.Init()
		{
			if (UseBsonSerializer && !Configure.Instance.Configurer.HasComponent<IMessageSerializer>())
				Configure.Instance.BsonSerializer();
		}
	}
}
