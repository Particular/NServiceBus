namespace NServiceBus
{
    using System.Linq;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.Json;

    public static class ConfigureJsonSerializer
  {
    public static Configure JsonSerializer(this Configure config)
    {
      ConfigureMessageMapper(config);

      config.Configurer.ConfigureComponent<JsonMessageSerializer>(DependencyLifecycle.SingleInstance);

      return config;
    }

    public static Configure BsonSerializer(this Configure config)
    {
      ConfigureMessageMapper(config);

      config.Configurer.ConfigureComponent<BsonMessageSerializer>(DependencyLifecycle.SingleInstance);

      return config;
    }

    private static void ConfigureMessageMapper(Configure config)
    {
      var messageTypes = Configure.TypesToScan.Where(t => MessageConventionExtensions.IsMessageType(t)).ToList();

      var messageMapper = new MessageMapper();
      messageMapper.Initialize(messageTypes);

      config.Configurer.RegisterSingleton<IMessageMapper>(messageMapper);
    }
  }
}