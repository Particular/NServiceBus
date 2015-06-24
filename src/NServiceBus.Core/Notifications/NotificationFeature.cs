namespace NServiceBus.Features
{
    using NServiceBus.Notifications;

    class NotificationFeature : Feature
    {
        public NotificationFeature()
        {
            EnableByDefault();
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var notifications = new BusNotifications();

            NotificationSubscriptions subscriptions;

            if (context.Settings.TryGet(out subscriptions))
            {
                subscriptions.ApplyTo(notifications);
            }

            context.Container.RegisterSingleton(notifications);
        }
    }
}