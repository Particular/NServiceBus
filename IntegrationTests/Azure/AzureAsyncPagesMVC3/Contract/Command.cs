using NServiceBus;

namespace Contract
{
    public class Command : ICommand
    {
        public int Id { get; set; }
    }
}
