using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Abstractions.Methods;

namespace RabbitMQ.Next.Abstractions.Channels
{
    public static class ChannelExtensions
    {
        public static Task<TResponse> SendAsync<TRequest, TResponse>(this IChannel channel, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : struct, IOutgoingMethod
            where TResponse : struct, IIncomingMethod
        {
            return channel.UseSyncChannel((request, cancellationToken), (ch, state) =>
                ch.SendAsync<TRequest, TResponse>(state.request, state.cancellationToken));
        }

        public static async Task<TResponse> SendAsync<TRequest, TResponse>(this ISynchronizedChannel channel, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : struct, IOutgoingMethod
            where TResponse : struct, IIncomingMethod
        {
            var waitTask = channel.WaitAsync<TResponse>(cancellationToken);
            await channel.SendAsync(request);
            return await waitTask;
        }
    }
}