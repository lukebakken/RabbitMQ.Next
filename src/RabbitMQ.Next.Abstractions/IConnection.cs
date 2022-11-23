using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Channels;

namespace RabbitMQ.Next;

public interface IConnection : IAsyncDisposable
{
    Task<IChannel> OpenChannelAsync(CancellationToken cancellationToken = default);

    ConnectionState State { get; }
}