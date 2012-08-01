using NServiceBus.ObjectBuilder;
using NServiceBus.Serialization;

namespace NServiceBus
{
	class SetJsonSerializerAsDefault : INeedInitialization
	{
		internal static bool UseJsonSerializer;

		void INeedInitialization.Init()
		{
			if (UseJsonSerializer && !Configure.Instance.Configurer.HasComponent<IMessageSerializer>())
				Configure.Instance.JsonSerializer();
		}
	}
}
