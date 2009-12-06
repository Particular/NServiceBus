using StructureMap.Attributes;
using StructureMap.Configuration.DSL;

namespace Barista
{
    public class BaristaRegistry : Registry
    {
        public BaristaRegistry()
        {
            ForRequestedType<IStarbucksBaristaView>()
                .CacheBy(InstanceScope.Singleton)
                .TheDefault.Is.OfConcreteType<StarbucksBarista>();

            ForRequestedType<IMessageSubscriptions>()
                .TheDefault.Is.OfConcreteType<MessageSubscriptions>();
        }
    }
}
