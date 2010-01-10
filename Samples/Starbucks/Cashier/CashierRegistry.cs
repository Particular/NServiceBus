using StructureMap.Configuration.DSL;

namespace Cashier
{
    public class CashierRegistry : Registry
    {
        public CashierRegistry()
        {
            For<IStarbucksCashierView>()
                .Singleton()
                .Use<StarbucksCashier>();  
        }
    }
}
