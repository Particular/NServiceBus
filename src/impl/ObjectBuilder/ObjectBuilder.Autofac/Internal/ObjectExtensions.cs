using System.Reflection;

namespace NServiceBus.ObjectBuilder.Autofac.Internal
{
    ///<summary>
    /// Object extension methods
    ///</summary>
    internal static class ObjectExtensions
    {
        ///<summary>
        /// Set a property value on an instance using reflection
        ///</summary>
        ///<param name="instance"></param>
        ///<param name="propertyName"></param>
        ///<param name="value"></param>
        public static void SetPropertyValue(this object instance, string propertyName, object value)
        {
            instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).SetValue(instance, value, null);
        }

    }
}