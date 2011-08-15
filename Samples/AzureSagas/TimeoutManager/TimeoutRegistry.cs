using StructureMap.Configuration.DSL;

namespace TimeoutManager
{
    public class TimeoutRegistry : Registry
    {
        public TimeoutRegistry()
        {
            For<IStarbucksTimeoutManagerView>()
                .Singleton()
                .Use<StarbucksTimeoutManager>();  
            
        }
    }
}
