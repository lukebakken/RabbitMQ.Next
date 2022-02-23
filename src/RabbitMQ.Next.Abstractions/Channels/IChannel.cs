using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Next.Messaging;
using RabbitMQ.Next.Methods;

namespace RabbitMQ.Next.Channels
{
    public interface IChannel
    {
        void AddFrameHandler(IFrameHandler handler);

        bool RemoveFrameHandler(IFrameHandler handler);

        Task Completion { get; }

        ValueTask SendAsync<TRequest>(TRequest request, CancellationToken cancellation = default)
            where TRequest : struct, IOutgoingMethod;

        ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellation = default)
            where TRequest : struct, IOutgoingMethod
            where TResponse : struct, IIncomingMethod;

        ValueTask PublishAsync<TState>(TState state, string exchange, string routingKey, IMessageProperties properties, Action<TState, IBufferWriter<byte>> payload, PublishFlags flags = PublishFlags.None, CancellationToken cancellation = default);

        ValueTask<TMethod> WaitAsync<TMethod>(CancellationToken cancellation = default)
            where TMethod : struct, IIncomingMethod;

        ValueTask CloseAsync(Exception ex = null);

        ValueTask CloseAsync(ushort statusCode, string description, MethodId failedMethodId);
    }
}