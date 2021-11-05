using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions.Channels;

namespace RabbitMQ.Next.TopologyBuilder.Commands
{
    internal interface ICommand
    {
        Task ExecuteAsync(IChannel channel);
    }
}