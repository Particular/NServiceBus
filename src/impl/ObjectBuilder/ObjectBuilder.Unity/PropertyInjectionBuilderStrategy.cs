namespace NServiceBus.ObjectBuilder.Unity
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.Practices.Unity;
    using Microsoft.Practices.Unity.ObjectBuilder;

    public class PropertyInjectionBuilderStrategy : BuilderStrategy
    {
        private readonly IUnityContainer _unityContainer;
        static IDictionary<Type, List<Tuple<string, object>>> configuredProperties = new Dictionary<Type, List<Tuple<string, object>>>();
 
        public PropertyInjectionBuilderStrategy(IUnityContainer unityContainer)
        {
            _unityContainer = unityContainer;
        }

        public static void SetPropertyValue(Type type, string name,object value)
        {
            if(!configuredProperties.ContainsKey(type))
                configuredProperties.Add(type,new List<Tuple<string, object>>());

            configuredProperties[type].Add(new Tuple<string, object>(name,value));
        }

        public override void PreBuildUp(IBuilderContext context)
        {
            var type = context.BuildKey.Type;
            if (!type.FullName.StartsWith("Microsoft.Practices"))
            {
                var properties = type.GetProperties();

              
                foreach (var property in properties)
                {
                    if (!property.CanWrite)
                        continue;

                    if (_unityContainer.IsRegistered(property.PropertyType))
                    {
                        property.SetValue(context.Existing, _unityContainer.Resolve(property.PropertyType),null);
                    }

                    if(configuredProperties.ContainsKey(type))
                    {
                        var p = configuredProperties[type].FirstOrDefault(t => t.Item1 == property.Name);

                        if(p != null)
                            property.SetValue(context.Existing, p.Item2,null);
                    }
                }
            }
        }
    }



    public class PropertyInjectionContainerExtension : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Context.Strategies.Add(new PropertyInjectionBuilderStrategy(Container), UnityBuildStage.Initialization);
        }
    }
}