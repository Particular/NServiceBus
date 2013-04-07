namespace NServiceBus.Config
{
    using Features;
    using Settings;

    /// <summary>
    /// 
    /// </summary>
    public class Feature
    {
        public static void Enable<T>() where T:IFeature
        {
            SettingsHolder.Set(typeof(T).FullName,true);
        }

        public static bool IsEnabled<T>() where T : IFeature
        {
            return SettingsHolder.GetOrDefault<bool>(typeof (T).FullName);
        }
    }
}