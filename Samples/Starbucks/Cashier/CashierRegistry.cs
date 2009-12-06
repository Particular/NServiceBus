using StructureMap.Attributes;
using StructureMap.Configuration.DSL;

namespace Cashier
{
    public class CashierRegistry : Registry
    {
        public CashierRegistry()
        {
            ForRequestedType<IStarbucksCashierView>()
                .CacheBy(InstanceScope.Singleton)
                .TheDefault.Is.OfConcreteType<StarbucksCashier>();  
        }
    }
}
