using StructureMap.Attributes;
using StructureMap.Configuration.DSL;

namespace Barista
{
    public class BaristaRegistry : Registry
    {
        public BaristaRegistry()
        {
            For<IStarbucksBaristaView>()
                .Singleton()
                .Use<StarbucksBarista>();

            For<IMessageSubscriptions>()
                .Use<MessageSubscriptions>();
        }
    }
}
