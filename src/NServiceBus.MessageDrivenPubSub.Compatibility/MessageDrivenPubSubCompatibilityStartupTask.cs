
namespace NServiceBus.MessageDrivenPubSub.Compatibility
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Settings;
    using Transport;

    public class MessageDrivenPubSubCompatibility : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.RegisterStartupTask(sp => new MessageDrivenPubSubCompatibilityStartupTask(context.Settings));
        }
    }

    public class MessageDrivenPubSubCompatibilityStartupTask : FeatureStartupTask
    {
        readonly IReadOnlySettings contextSettings;
        TransportDefinition transportDefinition;

        public MessageDrivenPubSubCompatibilityStartupTask(IReadOnlySettings contextSettings)
        {
            this.contextSettings = contextSettings;
        }

        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            var method = contextSettings.GetType().GetMethods()
                .Where(x => x.Name == nameof(contextSettings.Get))
                .FirstOrDefault(x => x.IsGenericMethod && x.GetParameters().Length == 0);
            if (method != null)
            {
                var seamType = Type.GetType("NServiceBus.TransportSeam, NServiceBus.Core, Version=8.0.0.0, Culture=neutral, PublicKeyToken=9fc386479f8a226c");
                var settingsType = seamType.GetNestedType("Settings");

                var genericMethod = method.MakeGenericMethod(settingsType);
                var settings = genericMethod.Invoke(contextSettings, null);
                var property = settings.GetType().GetProperty("TransportDefinition");

                transportDefinition = property.GetValue(settings) as TransportDefinition;
            }

            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
