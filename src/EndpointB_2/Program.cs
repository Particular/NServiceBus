using EndpointB;

namespace EndpointB_2
{
    class Program
    {
        static void Main(string[] args)
        {
            Configuration.Start("2").GetAwaiter().GetResult();
        }
    }
}
