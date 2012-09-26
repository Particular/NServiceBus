using System.Linq;
using NServiceBus.MessageInterfaces;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serializers.Json;

namespace NServiceBus
{
  public static class ConfigureJsonSerializer
  {
    public static Configure JsonSerializer(this Configure config)
    {
		if (!Configure.BuilderIsConfigured())
		{
			SetJsonSerializerAsDefault.UseJsonSerializer = true;
			return config;
		}

      ConfigureMessageMapper(config);

      config.Configurer.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance);

      return config;
    }

    public static Configure BsonSerializer(this Configure config)
    {
		if (!Configure.BuilderIsConfigured())
		{
			SetBsonSerializerAsDefault.UseBsonSerializer = true;
			return config;
		}

      ConfigureMessageMapper(config);

      config.Configurer.ConfigureComponent<BsonMessageSerializer>(DependencyLifecycle.SingleInstance);

      return config;
    }

    private static void ConfigureMessageMapper(Configure config)
    {
      var messageTypes = Configure.TypesToScan.Where(t => t.IsMessageType()).ToList();

      var messageMapper = new MessageMapper();
      messageMapper.Initialize(messageTypes);

      config.Configurer.RegisterSingleton<IMessageMapper>(messageMapper);
    }
  }
}