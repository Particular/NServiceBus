using System;
using NServiceBus;

namespace Contracts.Commands
{
    public class DemoCommand : ICommand
    {
        public Guid CommandId { get; set; }
    }
}